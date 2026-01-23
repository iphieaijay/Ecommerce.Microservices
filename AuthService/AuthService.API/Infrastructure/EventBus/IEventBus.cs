namespace AuthService.Infrastructure.EventBus
{
   
    /// <summary>
    /// Marker interface for domain events.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets the unique identifier for the event.
        /// </summary>
        Guid EventId => Guid.NewGuid();

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        DateTime OccurredAt => DateTime.UtcNow;
    }

    /// <summary>
    /// Interface for publishing domain events to a message broker.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to the message broker.
        /// </summary>
        /// <typeparam name="T">The type of event to publish.</typeparam>
        /// <param name="event">The event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;

        /// <summary>
        /// Publishes multiple events in a batch.
        /// </summary>
        /// <typeparam name="T">The type of events to publish.</typeparam>
        /// <param name="events">The collection of events to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : IEvent;
    }

    

}
