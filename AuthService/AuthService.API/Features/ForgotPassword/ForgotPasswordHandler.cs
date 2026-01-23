using AuthService.Domain;
using AuthService.Features.ForgotPassword;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ForgotPasswordHandler> _logger;
    private readonly IConfiguration _configuration;

    public ForgotPasswordHandler(UserManager<AppUser> userManager,IEmailService emailService,
        IEventBus eventBus,ILogger<ForgotPasswordHandler> logger, IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Security: Don't reveal if user exists or not
        // Always return success to prevent user enumeration attacks
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return new ForgotPasswordResponse(
                true,
                "If an account with that email exists, we've sent password reset instructions."
            );
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset requested for inactive user: {UserId}", user.Id);
            return new ForgotPasswordResponse(
                true,
                "If an account with that email exists, we've sent password reset instructions."
            );
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Password reset requested for unconfirmed email: {UserId}", user.Id);

            // Generate new confirmation token and send it instead
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedConfirmToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
            var confirmationLink = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedConfirmToken}";

            await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

            return new ForgotPasswordResponse(
                true,
                "Please confirm your email address first. We've sent you a new confirmation link."
            );
        }

        // Generate password reset token
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

        // Create reset link
        var baseUrlForReset = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
        var resetLink = $"{baseUrlForReset}/api/auth/reset-password?email={user.Email}&token={encodedToken}";

        // Send password reset email
        await _emailService.SendPasswordResetAsync(email: user.Email!, resetLink);

        // Publish domain event
        await _eventBus.PublishAsync(new PasswordResetRequestedEvent(
            user.Id,
            user.Email ?? string.Empty,
            DateTime.UtcNow
        ), cancellationToken);

        _logger.LogInformation("Password reset requested for user: {UserId}, {Email}", user.Id, user.Email);

        return new ForgotPasswordResponse(
            true,
            "If an account with that email exists, we've sent password reset instructions."
        );
    }
}
