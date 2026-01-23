using AuthService.Infrastructure.Services;
using System.Net;
using System.Net.Mail;

namespace AuthService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Confirm Your Email Address";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Email Confirmation</h2>
                    <p>Thank you for registering! Please confirm your email address by clicking the link below:</p>
                    <p style='margin: 30px 0;'>
                        <a href='{confirmationLink}' 
                           style='background-color: #007bff; color: white; padding: 12px 24px; 
                                  text-decoration: none; border-radius: 4px; display: inline-block;'>
                            Confirm Email
                        </a>
                    </p>
                    <p>If you didn't create an account, please ignore this email.</p>
                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                        This link will expire in 3 hours.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password. Click the link below to create a new password:</p>
                    <p style='margin: 30px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #dc3545; color: white; padding: 12px 24px; 
                                  text-decoration: none; border-radius: 4px; display: inline-block;'>
                            Reset Password
                        </a>
                    </p>
                    <p>If you didn't request a password reset, please ignore this email or contact support if you have concerns.</p>
                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                        This link will expire in 3 hours for security reasons.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordChangedNotificationAsync(string email, string userName)
    {
        var subject = "Password Changed Successfully";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Password Changed</h2>
                    <p>Hi {userName},</p>
                    <p>Your password has been successfully changed.</p>
                    <p>If you didn't make this change, please contact our support team immediately.</p>
                    <p style='margin-top: 30px;'>Best regards,<br/>The Security Team</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var subject = "Welcome to Our Platform!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Welcome, {userName}!</h2>
                    <p>Your email has been confirmed and your account is now active.</p>
                    <p>We're excited to have you on board!</p>
                    <p style='margin-top: 30px;'>Best regards,<br/>The Team</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            var smtpUsername = emailSettings["SmtpUsername"];
            var smtpPassword = emailSettings["SmtpPassword"];
            var fromEmail = emailSettings["FromEmail"];
            var fromName = emailSettings["FromName"];
            //var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

            // In development, just log the email
            if (string.IsNullOrEmpty(smtpHost) || smtpHost == "localhost")
            {
                _logger.LogInformation(
                    "Email would be sent to {To} with subject '{Subject}'. Body: {Body}",
                    to, subject, body);
                return;
            }

            // Validate configuration
            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("SMTP credentials not configured");
                throw new InvalidOperationException("Email service is not properly configured");
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                //EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 20000 // 20 seconds timeout
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "noreply@authservice.com", fromName ?? "Auth Service"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            _logger.LogInformation("Attempting to send email to {To} via {SmtpHost}:{SmtpPort}",
                to, smtpHost, smtpPort);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx,
                "SMTP error sending email to {To}. StatusCode: {StatusCode}",
                to, smtpEx.StatusCode);
            throw; 
        }
        catch (IOException ioEx)
        {
            _logger.LogError(ioEx,
                "IO error sending email to {To}. This may indicate SSL/TLS issues or network problems",
                to);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {To}", to);
            throw;
        }
    }
}
