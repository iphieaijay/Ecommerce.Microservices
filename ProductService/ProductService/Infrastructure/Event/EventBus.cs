using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ProductService.Infrastructure.Event.EventBus;

public interface IEvent
{
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
}

// RabbitMQ Implementation
public class EventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<EventBus> _logger;
    private readonly string _exchangeName = "product-service-events";

    public EventBus(IConfiguration configuration, ILogger<EventBus> logger)
    {
        _logger = logger;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("Event bus initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize event bus");
            throw;
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        try
        {
            var eventType = @event.GetType().Name;
            var routingKey = $"product.{eventType.ToLower()}";

            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = eventType;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} with routing key {RoutingKey}",
                eventType,
                routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("Event bus disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing event bus");
        }
    }
}

// Alternative: In-Memory Event Bus for Testing/Development
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly List<object> _publishedEvents = new();

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _publishedEvents.Add(@event);

        _logger.LogInformation(
            "Published event {EventType} to in-memory bus",
            typeof(T).Name);

        return Task.CompletedTask;
    }

    public IReadOnlyList<object> GetPublishedEvents() => _publishedEvents.AsReadOnly();

    public void Clear() => _publishedEvents.Clear();
}