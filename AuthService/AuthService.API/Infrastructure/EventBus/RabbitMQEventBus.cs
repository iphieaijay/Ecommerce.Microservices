
    using Polly;
    using Polly.Retry;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
namespace AuthService.Infrastructure.EventBus
{
  
    /// <summary>
    /// RabbitMQ implementation of the event bus with resilience and reconnection support.
    /// </summary>
    public class RabbitMQEventBus : IEventBus, IEventBusHealthCheck, IDisposable
    {
        private IConnection? _connection;
        private IModel? _channel;
        private readonly ILogger<RabbitMQEventBus> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _exchangeName;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly object _connectionLock = new();

        // Health tracking
        private bool _isConnected = false;
        private DateTime? _lastConnectedAt;
        private DateTime? _lastFailureAt;
        private string? _lastError;
        private int _eventsPublished = 0;
        private int _publishFailures = 0;

        public RabbitMQEventBus(IConfiguration configuration, ILogger<RabbitMQEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "auth-service-events";

            // Configure retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .Or<AlreadyClosedException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Failed to connect to RabbitMQ. Retry attempt {RetryCount} after {Delay}s",
                            retryCount,
                            timeSpan.TotalSeconds);
                    });

            TryConnect();
        }

        private void TryConnect()
        {
            lock (_connectionLock)
            {
                try
                {
                    if (_connection?.IsOpen == true)
                    {
                        return;
                    }

                    var factory = new ConnectionFactory
                    {
                        HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                        Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                        UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                        Password = _configuration["RabbitMQ:Password"] ?? "guest",
                        VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                        RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                        SocketReadTimeout = TimeSpan.FromSeconds(30),
                        SocketWriteTimeout = TimeSpan.FromSeconds(30),
                        RequestedHeartbeat = TimeSpan.FromSeconds(60),
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection("AuthService");
                    _channel = _connection.CreateModel();

                    // Configure connection event handlers
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    // Declare exchange as durable and topic-based
                    _channel.ExchangeDeclare(
                        exchange: _exchangeName,
                        type: ExchangeType.Topic,
                        durable: true,
                        autoDelete: false,
                        arguments: null);

                    // Enable publisher confirms for reliability
                    _channel.ConfirmSelect();

                    _isConnected = true;
                    _lastConnectedAt = DateTime.UtcNow;
                    _lastError = null;

                    _logger.LogInformation(
                        "Successfully connected to RabbitMQ at {HostName}:{Port}, Exchange: {ExchangeName}",
                        factory.HostName,
                        factory.Port,
                        _exchangeName);
                }
                catch (BrokerUnreachableException ex)
                {
                    _isConnected = false;
                    _lastFailureAt = DateTime.UtcNow;
                    _lastError = ex.Message;

                    _logger.LogError(
                        ex,
                        "RabbitMQ broker is unreachable at {HostName}:{Port}. Events will be logged but not published.",
                        _configuration["RabbitMQ:HostName"] ?? "localhost",
                        _configuration["RabbitMQ:Port"] ?? "5672");
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    _lastFailureAt = DateTime.UtcNow;
                    _lastError = ex.Message;

                    _logger.LogError(ex, "Failed to initialize RabbitMQ connection. Events will be logged but not published.");
                }
            }
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var eventType = @event.GetType().Name;
            var eventData = SerializeEvent(@event);

            // Always log the event
            _logger.LogInformation(
                "Publishing event {EventType} with ID {EventId}",
                eventType,
                @event.EventId);

            if (!_isConnected || _channel == null || _channel.IsClosed)
            {
                TryConnect();
            }

            if (!_isConnected)
            {
                _publishFailures++;
                _logger.LogWarning(
                    "RabbitMQ not connected. Event {EventType} (ID: {EventId}) logged but not published to message broker. Event Data: {EventData}",
                    eventType,
                    @event.EventId,
                    eventData);
                return;
            }

            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    EnsureChannelIsOpen();

                    var routingKey = $"auth.{eventType.ToLower().Replace("event", "")}";
                    var body = Encoding.UTF8.GetBytes(eventData);

                    var properties = _channel!.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.ContentEncoding = "UTF-8";
                    properties.Type = eventType;
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.MessageId = @event.EventId.ToString();
                    properties.AppId = "AuthService";
                    properties.DeliveryMode = 2; // Persistent

                    // Add custom headers
                    properties.Headers = new Dictionary<string, object>
                {
                    { "event-type", eventType },
                    { "occurred-at", @event.OccurredAt.ToString("o") },
                    { "source-service", "AuthService" }
                };

                    _channel.BasicPublish(
                        exchange: _exchangeName,
                        routingKey: routingKey,
                        mandatory: false,
                        basicProperties: properties,
                        body: body);

                    // Wait for confirmation
                    _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                    _eventsPublished++;

                    _logger.LogInformation(
                        "Successfully published event {EventType} (ID: {EventId}) with routing key {RoutingKey}",
                        eventType,
                        @event.EventId,
                        routingKey);

                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _publishFailures++;
                _lastFailureAt = DateTime.UtcNow;
                _lastError = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed to publish event {EventType} (ID: {EventId}) to RabbitMQ after retries. Event logged but not published. Event Data: {EventData}",
                    eventType,
                    @event.EventId,
                    eventData);

                // Don't throw - allow application to continue
            }
        }

        public async Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : IEvent
        {
            if (events == null || !events.Any())
            {
                return;
            }

            var eventList = events.ToList();
            _logger.LogInformation("Publishing batch of {Count} events", eventList.Count);

            if (!_isConnected || _channel == null || _channel.IsClosed)
            {
                TryConnect();
            }

            if (!_isConnected)
            {
                _logger.LogWarning("RabbitMQ not connected. Batch of {Count} events logged but not published", eventList.Count);
                foreach (var evt in eventList)
                {
                    await PublishAsync(evt, cancellationToken);
                }
                return;
            }

            try
            {
                EnsureChannelIsOpen();

                // Use a transaction for batch publishing
                _channel!.TxSelect();

                foreach (var @event in eventList)
                {
                    var eventType = @event.GetType().Name;
                    var eventData = SerializeEvent(@event);
                    var routingKey = $"auth.{eventType.ToLower().Replace("event", "")}";
                    var body = Encoding.UTF8.GetBytes(eventData);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.Type = eventType;
                    properties.MessageId = @event.EventId.ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.Headers = new Dictionary<string, object>
                {
                    { "event-type", eventType },
                    { "occurred-at", @event.OccurredAt.ToString("o") },
                    { "source-service", "AuthService" }
                };

                    _channel.BasicPublish(
                        exchange: _exchangeName,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body);
                }

                _channel.TxCommit();
                _eventsPublished += eventList.Count;

                _logger.LogInformation("Successfully published batch of {Count} events", eventList.Count);
            }
            catch (Exception ex)
            {
                try
                {
                    _channel?.TxRollback();
                }
                catch
                {
                    // Ignore rollback errors
                }

                _publishFailures += eventList.Count;
                _logger.LogError(ex, "Failed to publish batch of {Count} events", eventList.Count);

                // Fall back to individual publishing
                foreach (var evt in eventList)
                {
                    await PublishAsync(evt, cancellationToken);
                }
            }

            await Task.CompletedTask;
        }

        private void EnsureChannelIsOpen()
        {
            if (_channel == null || _channel.IsClosed)
            {
                TryConnect();

                if (_channel == null || _channel.IsClosed)
                {
                    throw new InvalidOperationException("RabbitMQ channel is not available");
                }
            }
        }

        private string SerializeEvent<T>(T @event) where T : IEvent
        {
            return JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        // Event handlers
        private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            _isConnected = false;
            _lastFailureAt = DateTime.UtcNow;
            _lastError = $"Connection shutdown: {e.ReplyText}";

            _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
            TryConnect();
        }

        private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "RabbitMQ callback exception");
        }

        private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection blocked: {Reason}", e.Reason);
        }

        // Health check implementation
        public Task<bool> IsHealthyAsync()
        {
            return Task.FromResult(_isConnected && _connection?.IsOpen == true && _channel?.IsOpen == true);
        }

        public Task<EventBusStatus> GetStatusAsync()
        {
            return Task.FromResult(new EventBusStatus
            {
                IsConnected = _isConnected,
                ConnectionType = "RabbitMQ",
                LastConnectedAt = _lastConnectedAt,
                LastFailureAt = _lastFailureAt,
                LastError = _lastError,
                EventsPublished = _eventsPublished,
                PublishFailures = _publishFailures
            });
        }

        public void Dispose()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.IsOpen)
                    {
                        _channel.Close();
                    }
                    _channel.Dispose();
                }

                if (_connection != null)
                {
                    if (_connection.IsOpen)
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                }

                _logger.LogInformation("RabbitMQ event bus disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ event bus");
            }
        }
    }
}
