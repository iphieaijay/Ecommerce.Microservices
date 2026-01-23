using AuthService.Infrastructure.EventBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace AuthService.Infrastructure.EventBus
{
   
    /// <summary>
    /// Factory for creating event bus instances.
    /// </summary>
    public static class EventBusFactory
    {
        /// <summary>
        /// Creates an event bus instance based on configuration.
        /// </summary>
        public static IEventBus CreateEventBus(
            IConfiguration configuration,
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            var useInMemory = configuration.GetValue<bool>("UseInMemoryEventBus", false);

            if (useInMemory)
            {
                logger.LogInformation("Using InMemoryEventBus for development/testing");
                return new InMemoryEventBus(
                    serviceProvider.GetRequiredService<ILogger<InMemoryEventBus>>());
            }

            logger.LogInformation("Using RabbitMQEventBus for production");
            return new RabbitMQEventBus(
                configuration,
                serviceProvider.GetRequiredService<ILogger<RabbitMQEventBus>>());
        }
    }

    /// <summary>
    /// Extension methods for registering event bus services.
    /// </summary>
    public static class EventBusServiceExtensions
    {
        /// <summary>
        /// Adds event bus services to the service collection.
        /// </summary>
        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            var useInMemory = configuration.GetValue<bool>("UseInMemoryEventBus", false);

            if (useInMemory)
            {
                services.AddSingleton<IEventBus, InMemoryEventBus>();
                services.AddSingleton<IEventBusHealthCheck>(sp =>
                    (InMemoryEventBus)sp.GetRequiredService<IEventBus>());
            }
            else
            {
                services.AddSingleton<IEventBus, RabbitMQEventBus>();
                services.AddSingleton<IEventBusHealthCheck>(sp =>
                    (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());
            }

            return services;
        }

        /// <summary>
        /// Adds event bus health checks.
        /// </summary>
        public static IHealthChecksBuilder AddEventBusHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddCheck<EventBusHealthCheck>("eventbus", tags: new[] { "eventbus", "messaging" });
        }
    }

    /// <summary>
    /// Health check for the event bus.
    /// </summary>
    public class EventBusHealthCheck : IHealthCheck
    {
        private readonly IEventBusHealthCheck _eventBusHealthCheck;
        private readonly ILogger<EventBusHealthCheck> _logger;

        public EventBusHealthCheck(
            IEventBusHealthCheck eventBusHealthCheck,
            ILogger<EventBusHealthCheck> logger)
        {
            _eventBusHealthCheck = eventBusHealthCheck;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _eventBusHealthCheck.IsHealthyAsync();
                var status = await _eventBusHealthCheck.GetStatusAsync();

                var data = new Dictionary<string, object>
            {
                { "connection_type", status.ConnectionType },
                { "is_connected", status.IsConnected },
                { "events_published", status.EventsPublished },
                { "publish_failures", status.PublishFailures }
            };

                if (status.LastConnectedAt.HasValue)
                {
                    data.Add("last_connected_at", status.LastConnectedAt.Value);
                }

                if (status.LastFailureAt.HasValue)
                {
                    data.Add("last_failure_at", status.LastFailureAt.Value);
                }

                if (!string.IsNullOrEmpty(status.LastError))
                {
                    data.Add("last_error", status.LastError);
                }

                if (isHealthy)
                {
                    return HealthCheckResult.Healthy(
                        $"Event bus ({status.ConnectionType}) is healthy",
                        data);
                }

                // Degraded if not connected but application can continue
                return HealthCheckResult.Degraded(
                    $"Event bus ({status.ConnectionType}) is not connected but application is operational",
                    data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event bus health check failed");

                return HealthCheckResult.Degraded("Event bus health check failed",ex, new Dictionary<string, object> { { "error", ex.Message } });
            }
        }
    }
}
