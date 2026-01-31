// NotificationService.Api/Infrastructure/Email/SmtpEmailSender.cs
using Microsoft.Extensions.Options;
using NotificationService.Api.Infrastructure.NotificationService.Api.Infrastructure.Email;
using NotificationService.Api.Infrastructure.NotificationService.Api.Infrastructure.Email.NotificationService.Api.Infrastructure.Email;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using System.Net.Mail;

namespace NotificationService.Api.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly SemaphoreSlim _semaphore;

    public SmtpEmailSender(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_settings.MaxConcurrentSends);

        // Circuit breaker: open after 5 failures, stay open for 30 seconds
        _circuitBreaker = Policy
            .Handle<SmtpException>()
            .Or<InvalidOperationException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                {
                    _logger.LogError(ex, "Circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                });
    }

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml,
        string? cc = null,
        string? bcc = null,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            ValidateEmailAddress(to);

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                using var client = CreateSmtpClient();
                using var message = CreateMailMessage(to, subject, body, isHtml, cc, bcc);

                await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Email sent successfully to {To} with subject: {Subject}",
                    to, subject);

                return new EmailSendResult(true);
            });
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning("Circuit breaker is open, email send rejected");
            return new EmailSendResult(false, "Email service temporarily unavailable");
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {To}", to);
            return new EmailSendResult(false, $"SMTP Error: {ex.Message}");
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid email format: {To}", to);
            return new EmailSendResult(false, $"Invalid email format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {To}", to);
            return new EmailSendResult(false, $"Unexpected error: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Timeout = _settings.TimeoutSeconds * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        return client;
    }

    private MailMessage CreateMailMessage(
        string to,
        string subject,
        string body,
        bool isHtml,
        string? cc,
        string? bcc)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        // Add recipients
        foreach (var email in SplitEmails(to))
        {
            message.To.Add(email);
        }

        if (!string.IsNullOrWhiteSpace(cc))
        {
            foreach (var email in SplitEmails(cc))
            {
                message.CC.Add(email);
            }
        }

        if (!string.IsNullOrWhiteSpace(bcc))
        {
            foreach (var email in SplitEmails(bcc))
            {
                message.Bcc.Add(email);
            }
        }

        return message;
    }

    private static IEnumerable<string> SplitEmails(string emails)
    {
        return emails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e));
    }

    private static void ValidateEmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new FormatException("Email address cannot be empty");

        var emails = SplitEmails(email);
        foreach (var addr in emails)
        {
            try
            {
                var mailAddress = new MailAddress(addr);
            }
            catch (FormatException)
            {
                throw new FormatException($"Invalid email address: {addr}");
            }
        }
    }
}
