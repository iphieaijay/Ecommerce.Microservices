namespace NotificationService.Domain.Entities
{
    public class EmailNotification
    {
        public Guid Id { get; set; }
        public string To { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public NotificationStatus Status { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? TemplateId { get; set; }
        public Dictionary<string, object>? TemplateData { get; set; }
        public NotificationPriority Priority { get; set; }
        public string? CorrelationId { get; set; }

        public bool CanRetry() => RetryCount < MaxRetries && Status == NotificationStatus.Failed;

        public void MarkAsSent()
        {
            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void MarkAsFailed(string error)
        {
            Status = NotificationStatus.Failed;
            ErrorMessage = error;
            RetryCount++;
        }

        public void MarkAsPending()
        {
            Status = NotificationStatus.Pending;
        }
    }

    public enum NotificationStatus
    {
        Pending,
        Processing,
        Sent,
        Failed,
        Cancelled
    }

    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

}
