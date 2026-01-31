// NotificationService.Api/Infrastructure/InfrastructureExtensions.cs
using Microsoft.Extensions.Options;
using NotificationService.Api.Infrastructure.Email;
using NotificationService.Api.Infrastructure.NotificationService.Api.Infrastructure.Email;
using NotificationService.Api.Infrastructure.RateLimiting;
using NotificationService.Api.Infrastructure.Resilience;
using System.Threading.RateLimiting;

namespace NotificationService.Api.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Email Configuration
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Circuit Breaker & Resilience
        services.AddResiliencePolicies();

        // Rate Limiting
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("cache");
            options.InstanceName = "NotificationService_";
        });

        // Background Services
        services.AddHostedService<EmailProcessorBackgroundService>();
        services.AddHostedService<ScheduledEmailProcessorService>();
        services.AddHostedService<RetryFailedEmailsService>();

        return services;
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;

        // Auto-register all feature handlers
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}

// NotificationService.Api/Infrastructure/Email/EmailSettings.cs
namespace NotificationService.Api.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentSends { get; set; } = 10;
}

// NotificationService.Api/Infrastructure/Email/IEmailSender.cs
namespace NotificationService.Api.Infrastructure.Email;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml,
        string? cc = null,
        string? bcc = null,
        CancellationToken cancellationToken = default);
}

public record EmailSendResult(bool Success, string? ErrorMessage = null);