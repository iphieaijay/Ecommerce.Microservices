using AuthService.Domain;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace AuthService.Features.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(UserManager<AppUser> userManager, IEmailService emailService,
        ITokenBlacklistService tokenBlacklistService, IEventBus eventBus, ILogger<ResetPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _tokenBlacklistService = tokenBlacklistService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Password reset attempt for non-existent email: {Email}", request.Email);
            throw new KeyNotFoundException("User not found");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset attempt for inactive user: {UserId}", user.Id);
            throw new InvalidOperationException("Your account has been deactivated. Please contact support.");
        }

        // Decode token
        string decodedToken;
        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid reset token format for user: {Email}", request.Email);
            throw new InvalidOperationException("Invalid or corrupted reset token");
        }

        // Reset password
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Password reset failed for user {UserId}: {Errors}", user.Id, errors);

            // Check if token expired
            if (errors.Contains("Invalid token") || errors.Contains("expired"))
            {
                throw new InvalidOperationException("Reset link has expired. Please request a new password reset.");
            }

            throw new InvalidOperationException($"Password reset failed: {errors}");
        }

        // Invalidate all existing refresh tokens for security
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        // Reset failed access count
        await _userManager.ResetAccessFailedCountAsync(user);

        // Unlock account if locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        // Send password changed notification
        await _emailService.SendPasswordChangedNotificationAsync(user.Email ?? string.Empty, user.UserName ?? string.Empty);

        // Publish domain event
        await _eventBus.PublishAsync(new PasswordResetCompletedEvent(
            user.Id,
            user.Email ?? string.Empty,
            DateTime.UtcNow
        ), cancellationToken);

        _logger.LogInformation("Password reset successfully for user: {UserId}, {Email}", user.Id, user.Email);

        return new ResetPasswordResponse(
            true,
            "Password has been reset successfully. You can now log in with your new password."
        );
    }
}