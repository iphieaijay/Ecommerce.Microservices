namespace AuthService.Infrastructure.EventBus
{

    /// <summary>
    /// Represents the status of the event bus connection.
    /// </summary>
    public class EventBusStatus
    {
        public bool IsConnected { get; set; }
        public string ConnectionType { get; set; } = string.Empty;
        public DateTime? LastConnectedAt { get; set; }
        public DateTime? LastFailureAt { get; set; }
        public string? LastError { get; set; }
        public int EventsPublished { get; set; }
        public int PublishFailures { get; set; }
    }
}
