using AuthService.Infrastructure.EventBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers
{
    /// <summary>
    /// Controller for managing and monitoring the event bus.
    /// </summary>
    [ApiController]
    [Route("api/eventbus")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class EventBusController : ControllerBase
    {
        private readonly IEventBusHealthCheck _eventBusHealthCheck;
        private readonly IEventBus _eventBus;
        private readonly ILogger<EventBusController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusController"/> class.
        /// </summary>
        public EventBusController(
            IEventBusHealthCheck eventBusHealthCheck,
            IEventBus eventBus,
            ILogger<EventBusController> logger)
        {
            _eventBusHealthCheck = eventBusHealthCheck;
            _eventBus = eventBus;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current status of the event bus.
        /// </summary>
        /// <returns>Event bus status information.</returns>
        /// <response code="200">Status retrieved successfully.</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(EventBusStatus), StatusCodes.Status200OK)]
        public async Task<ActionResult<EventBusStatus>> GetStatus()
        {
            var status = await _eventBusHealthCheck.GetStatusAsync();
            return Ok(status);
        }

        /// <summary>
        /// Checks if the event bus is healthy.
        /// </summary>
        /// <returns>Health status.</returns>
        /// <response code="200">Health check completed.</response>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> CheckHealth()
        {
            var isHealthy = await _eventBusHealthCheck.IsHealthyAsync();
            var status = await _eventBusHealthCheck.GetStatusAsync();

            return Ok(new
            {
                healthy = isHealthy,
                status = status.IsConnected ? "connected" : "disconnected",
                connectionType = status.ConnectionType,
                eventsPublished = status.EventsPublished,
                publishFailures = status.PublishFailures
            });
        }

        /// <summary>
        /// Gets statistics for in-memory event bus (development only).
        /// </summary>
        /// <returns>Event bus statistics.</returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="404">Only available for in-memory event bus.</response>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(EventBusStatistics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<EventBusStatistics> GetStatistics()
        {
            if (_eventBus is InMemoryEventBus inMemoryBus)
            {
                var stats = inMemoryBus.GetStatistics();
                return Ok(stats);
            }

            return NotFound(new { error = "Statistics only available for in-memory event bus" });
        }

        /// <summary>
        /// Gets all published events from in-memory event bus (development only).
        /// </summary>
        /// <param name="eventType">Optional filter by event type.</param>
        /// <param name="limit">Maximum number of events to return.</param>
        /// <returns>List of published events.</returns>
        /// <response code="200">Events retrieved successfully.</response>
        /// <response code="404">Only available for in-memory event bus.</response>
        [HttpGet("events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetPublishedEvents(
            [FromQuery] string? eventType = null,
            [FromQuery] int limit = 100)
        {
            if (_eventBus is InMemoryEventBus inMemoryBus)
            {
                IReadOnlyList<PublishedEvent> events;

                if (!string.IsNullOrEmpty(eventType))
                {
                    events = inMemoryBus.GetPublishedEventsByType(eventType);
                }
                else
                {
                    events = inMemoryBus.GetPublishedEvents();
                }

                var limitedEvents = events
                    .OrderByDescending(e => e.PublishedAt)
                    .Take(limit)
                    .Select(e => new
                    {
                        e.EventId,
                        e.EventType,
                        e.OccurredAt,
                        e.PublishedAt,
                        e.EventData
                    });

                return Ok(new
                {
                    totalCount = events.Count,
                    returnedCount = limitedEvents.Count(),
                    events = limitedEvents
                });
            }

            return NotFound(new { error = "Event history only available for in-memory event bus" });
        }

        /// <summary>
        /// Clears all events from in-memory event bus (development only).
        /// </summary>
        /// <returns>Confirmation message.</returns>
        /// <response code="200">Events cleared successfully.</response>
        /// <response code="404">Only available for in-memory event bus.</response>
        [HttpDelete("events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult ClearEvents()
        {
            if (_eventBus is InMemoryEventBus inMemoryBus)
            {
                var countBefore = inMemoryBus.GetEventCount();
                inMemoryBus.Clear();

                _logger.LogInformation("Cleared {Count} events from in-memory event bus", countBefore);

                return Ok(new
                {
                    success = true,
                    message = $"Cleared {countBefore} events from in-memory event bus"
                });
            }

            return NotFound(new { error = "Clear operation only available for in-memory event bus" });
        }

        /// <summary>
        /// Publishes a test event to verify event bus functionality.
        /// </summary>
        /// <returns>Confirmation of test event publication.</returns>
        /// <response code="200">Test event published successfully.</response>
        [HttpPost("test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PublishTestEvent()
        {
            var testEvent = new TestEvent
            {
                TestId = Guid.NewGuid(),
                Message = "This is a test event from the EventBus controller",
                Timestamp = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(testEvent);

            _logger.LogInformation("Published test event with ID {TestId}", testEvent.TestId);

            return Ok(new
            {
                success = true,
                message = "Test event published successfully",
                eventId = testEvent.EventId,
                testId = testEvent.TestId
            });
        }
    }

    /// <summary>
    /// Test event for verifying event bus functionality.
    /// </summary>
    public record TestEvent : IEvent
    {
        public Guid EventId => Guid.NewGuid();
        public DateTime OccurredAt => DateTime.UtcNow;
        public Guid TestId { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }
}
