// NotificationService.Api/Infrastructure/Persistence/NotificationDbContext.cs
using Microsoft.EntityFrameworkCore;
using NotificationService.Api.Domain;
using NotificationService.Api.Infrastructure.Persistence.NotificationService.Api.Infrastructure.Persistence.NotificationService.Api.Infrastructure.Persistence;
using NotificationService.Api.Infrastructure.Persistence.NotificationService.Api.Infrastructure.Persistence.NotificationService.Api.Infrastructure.Persistence.NotificationService.Api.Infrastructure.Persistence;
using NotificationService.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.Json;

namespace NotificationService.Api.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt });

            entity.Property(e => e.To).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);

            // JSON columns for complex types
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");

            entity.Property(e => e.TemplateData)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");
        });
    }
}

// NotificationService.Api/Infrastructure/Persistence/PersistenceExtensions.cs
namespace NotificationService.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("notificationdb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                });

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        await context.Database.MigrateAsync();
    }
}

// NotificationService.Api/Infrastructure/Persistence/INotificationRepository.cs
namespace NotificationService.Api.Infrastructure.Persistence;

public interface INotificationRepository
{
    Task<EmailNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EmailNotification>> GetPendingNotificationsAsync(int take, CancellationToken cancellationToken = default);
    Task<List<EmailNotification>> GetScheduledNotificationsAsync(DateTime upTo, CancellationToken cancellationToken = default);
    Task<List<EmailNotification>> GetFailedNotificationsForRetryAsync(int take, CancellationToken cancellationToken = default);
    Task AddAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// NotificationService.Api/Infrastructure/Persistence/NotificationRepository.cs
namespace NotificationService.Api.Infrastructure.Persistence;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        NotificationDbContext context,
        ILogger<NotificationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailNotifications
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<EmailNotification>> GetPendingNotificationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailNotifications
            .Where(e => e.Status == NotificationStatus.Pending &&
                       (e.ScheduledFor == null || e.ScheduledFor <= DateTime.UtcNow))
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailNotification>> GetScheduledNotificationsAsync(
        DateTime upTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailNotifications
            .Where(e => e.Status == NotificationStatus.Pending &&
                       e.ScheduledFor != null &&
                       e.ScheduledFor <= upTo)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.ScheduledFor)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailNotification>> GetFailedNotificationsForRetryAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailNotifications
            .Where(e => e.Status == NotificationStatus.Failed &&
                       e.RetryCount < e.MaxRetries)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        await _context.EmailNotifications.AddAsync(notification, cancellationToken);
    }

    public Task UpdateAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        _context.EmailNotifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}