namespace AuthService.Infrastructure.EventBus
{
    /// <summary>
    /// Interface for event bus health monitoring.
    /// </summary>
    public interface IEventBusHealthCheck
    {
        /// <summary>
        /// Checks if the event bus is healthy and connected.
        /// </summary>
        /// <returns>True if healthy, false otherwise.</returns>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        /// <returns>Connection status information.</returns>
        Task<EventBusStatus> GetStatusAsync();
    }
}
