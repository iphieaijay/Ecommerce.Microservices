 using System.Collections.Concurrent;
 using System.Text.Json;

namespace AuthService.Infrastructure.EventBus
{
   
   
    /// <summary>
    /// In-memory implementation of the event bus for development and testing.
    /// </summary>
    public class InMemoryEventBus : IEventBus, IEventBusHealthCheck
    {
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly ConcurrentBag<PublishedEvent> _publishedEvents = new();
        private int _eventsPublished = 0;
        private readonly DateTime _startedAt = DateTime.UtcNow;

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("InMemoryEventBus initialized - events will be logged but not published to external broker");
        }

        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var eventType = @event.GetType().Name;
            var eventData = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var publishedEvent = new PublishedEvent
            {
                EventId = @event.EventId,
                EventType = eventType,
                OccurredAt = @event.OccurredAt,
                PublishedAt = DateTime.UtcNow,
                EventData = eventData,
                Event = @event
            };

            _publishedEvents.Add(publishedEvent);
            _eventsPublished++;

            _logger.LogInformation(
                "Published event to in-memory bus: {EventType} (ID: {EventId})\nEvent Data: {EventData}",
                eventType,
                @event.EventId,
                eventData);

            return Task.CompletedTask;
        }

        public async Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : IEvent
        {
            if (events == null || !events.Any())
            {
                return;
            }

            var eventList = events.ToList();
            _logger.LogInformation("Publishing batch of {Count} events to in-memory bus", eventList.Count);

            foreach (var @event in eventList)
            {
                await PublishAsync(@event, cancellationToken);
            }

            _logger.LogInformation("Successfully published batch of {Count} events", eventList.Count);
        }

        /// <summary>
        /// Gets all published events.
        /// </summary>
        public IReadOnlyList<PublishedEvent> GetPublishedEvents()
        {
            return _publishedEvents.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets published events of a specific type.
        /// </summary>
        public IReadOnlyList<PublishedEvent> GetPublishedEvents<T>() where T : IEvent
        {
            var eventType = typeof(T).Name;
            return _publishedEvents
                .Where(e => e.EventType == eventType)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets published events by event type name.
        /// </summary>
        public IReadOnlyList<PublishedEvent> GetPublishedEventsByType(string eventType)
        {
            return _publishedEvents
                .Where(e => e.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets the count of published events.
        /// </summary>
        public int GetEventCount()
        {
            return _eventsPublished;
        }

        /// <summary>
        /// Gets the count of published events by type.
        /// </summary>
        public int GetEventCount<T>() where T : IEvent
        {
            return GetPublishedEvents<T>().Count;
        }

        /// <summary>
        /// Clears all published events from memory.
        /// </summary>
        public void Clear()
        {
            _publishedEvents.Clear();
            _logger.LogInformation("Cleared all events from in-memory event bus");
        }

        /// <summary>
        /// Gets event statistics.
        /// </summary>
        public EventBusStatistics GetStatistics()
        {
            var eventsByType = _publishedEvents
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new EventBusStatistics
            {
                TotalEventsPublished = _eventsPublished,
                EventsInMemory = _publishedEvents.Count,
                EventTypeCount = eventsByType.Count,
                EventsByType = eventsByType,
                OldestEvent = _publishedEvents.OrderBy(e => e.PublishedAt).FirstOrDefault()?.PublishedAt,
                NewestEvent = _publishedEvents.OrderByDescending(e => e.PublishedAt).FirstOrDefault()?.PublishedAt,
                StartedAt = _startedAt,
                Uptime = DateTime.UtcNow - _startedAt
            };
        }

        // Health check implementation
        public Task<bool> IsHealthyAsync()
        {
            return Task.FromResult(true);
        }

        public Task<EventBusStatus> GetStatusAsync()
        {
            return Task.FromResult(new EventBusStatus
            {
                IsConnected = true,
                ConnectionType = "InMemory",
                LastConnectedAt = _startedAt,
                LastFailureAt = null,
                LastError = null,
                EventsPublished = _eventsPublished,
                PublishFailures = 0
            });
        }
    }

    /// <summary>
    /// Represents a published event in the in-memory store.
    /// </summary>
    public class PublishedEvent
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public DateTime PublishedAt { get; set; }
        public string EventData { get; set; } = string.Empty;
        public object Event { get; set; } = null!;
    }

    /// <summary>
    /// Statistics for the in-memory event bus.
    /// </summary>
    public class EventBusStatistics
    {
        public int TotalEventsPublished { get; set; }
        public int EventsInMemory { get; set; }
        public int EventTypeCount { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new();
        public DateTime? OldestEvent { get; set; }
        public DateTime? NewestEvent { get; set; }
        public DateTime StartedAt { get; set; }
        public TimeSpan Uptime { get; set; }
    }
}
