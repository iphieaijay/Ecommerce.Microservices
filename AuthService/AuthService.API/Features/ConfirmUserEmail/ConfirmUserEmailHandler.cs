using AuthService.Domain;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace AuthService.Features.ConfirmUserEmail
{
    public class ConfirmUserEmailHandler : IRequestHandler<ConfirmUserEmailCommand, ConfirmUserEmailResponse>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<ConfirmUserEmailHandler> _logger;

        public ConfirmUserEmailHandler(
            UserManager<AppUser> userManager,
            IEmailService emailService,
            IEventBus eventBus,
            ILogger<ConfirmUserEmailHandler> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<ConfirmUserEmailResponse> Handle(ConfirmUserEmailCommand request, CancellationToken cancellationToken)
        {
            // Find user
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Email confirmation attempt for non-existent user: {UserId}", request.UserId);
                throw new KeyNotFoundException("User not found");
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user: {UserId}", user.Id);
                return new ConfirmUserEmailResponse(
                    true,
                    "Email is already confirmed. You can now log in."
                );
            }

            // Decode token with better error handling
            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
                _logger.LogDebug("Token decoded successfully for user: {UserId}", request.UserId);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Token format error for user: {UserId}. Token: {Token}",
                    request.UserId, request.Token);
                throw new InvalidOperationException("Invalid confirmation token format. Please request a new confirmation email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error decoding token for user: {UserId}", request.UserId);
                throw new InvalidOperationException("Invalid or corrupted confirmation token");
            }

            // Confirm email
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Email confirmation failed for user {UserId}: {Errors}", user.Id, errors);

                // Log the actual token for debugging (remove in production)
                _logger.LogDebug("Failed token (first 20 chars): {Token}",
                    decodedToken.Length > 20 ? decodedToken.Substring(0, 20) : decodedToken);

                // Check specific error types
                var errorCodes = result.Errors.Select(e => e.Code).ToList();

                if (errorCodes.Contains("InvalidToken"))
                {
                    throw new InvalidOperationException(
                        "The confirmation link is invalid or has expired. Please request a new confirmation email.");
                }

                throw new InvalidOperationException($"Email confirmation failed: {errors}");
            }

            // Send welcome email (wrap in try-catch to not fail the confirmation)
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    user.Email ?? string.Empty,
                    user.UserName ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to user: {UserId}", user.Id);
                // Don't throw - email is confirmed even if welcome email fails
            }

            // Publish domain event
            try
            {
                await _eventBus.PublishAsync(new EmailConfirmedEvent(
                    user.Id,
                    user.Email ?? string.Empty,
                    DateTime.UtcNow
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish EmailConfirmedEvent for user: {UserId}", user.Id);
                // Don't throw - email is confirmed even if event publishing fails
            }

            _logger.LogInformation("Email confirmed successfully for user: {UserId}, {Email}",
                user.Id, user.Email);

            return new ConfirmUserEmailResponse(
                true,
                "Email confirmed successfully! You can now log in to your account."
            );
        }
        //public async Task<ConfirmUserEmailResponse> Handle(ConfirmUserEmailCommand request, CancellationToken cancellationToken)
        //{
        //    // Find user
        //    var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        //    if (user == null)
        //    {
        //        _logger.LogWarning("Email confirmation attempt for non-existent user: {UserId}", request.UserId);
        //        throw new KeyNotFoundException("User not found");
        //    }

        //    // Check if email is already confirmed
        //    if (user.EmailConfirmed)
        //    {
        //        _logger.LogInformation("Email already confirmed for user: {UserId}", user.Id);
        //        return new ConfirmUserEmailResponse(
        //            true,
        //            "Email is already confirmed. You can now log in."
        //        );
        //    }

        //    // Decode token
        //    string decodedToken;
        //    try
        //    {
        //        var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
        //        decodedToken = Encoding.UTF8.GetString(tokenBytes);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Invalid token format for user: {UserId}", request.UserId);
        //        throw new InvalidOperationException("Invalid or corrupted confirmation token");
        //    }

        //    // Confirm email
        //    var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        //    if (!result.Succeeded)
        //    {
        //        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        //        _logger.LogError("Email confirmation failed for user {UserId}: {Errors}", user.Id, errors);

        //        // Check if token expired
        //        if (errors.Contains("Invalid token") || errors.Contains("expired"))
        //        {
        //            throw new InvalidOperationException("Confirmation link has expired. Please request a new confirmation email.");
        //        }

        //        throw new InvalidOperationException($"Email confirmation failed: {errors}");
        //    }

        //    // Send welcome email
        //    await _emailService.SendWelcomeEmailAsync(user.Email ?? string.Empty, user.UserName ?? string.Empty);

        //    // Publish domain event
        //    await _eventBus.PublishAsync(new EmailConfirmedEvent(
        //        user.Id,
        //        user.Email ?? string.Empty,
        //        DateTime.UtcNow
        //    ), cancellationToken);

        //    _logger.LogInformation("Email confirmed successfully for user: {UserId}, {Email}", user.Id, user.Email);

        //    return new ConfirmUserEmailResponse(
        //        true,
        //        "Email confirmed successfully! You can now log in to your account."
        //    );
        //}

    }



}
