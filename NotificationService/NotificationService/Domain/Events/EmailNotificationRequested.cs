using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Events
{
    public record EmailNotificationRequested(
    Guid NotificationId,
    string To,
    string Subject,
    string Body,
    bool IsHtml = false,
    string? TemplateId = null,
    Dictionary<string, object>? TemplateData = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    DateTime? ScheduledFor = null,
    string? CorrelationId = null
);

    public record EmailNotificationSent(
        Guid NotificationId,
        string To,
        DateTime SentAt,
        string? CorrelationId = null
    );

    public record EmailNotificationFailed(
        Guid NotificationId,
        string To,
        string ErrorMessage,
        int RetryCount,
        bool WillRetry,
        string? CorrelationId = null
    );
}
