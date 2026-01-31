using Microsoft.AspNetCore.Connections;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;
    using System.Text.Json;

    namespace OrderService.Infrastructure.EventBus
    {
        public interface IRabbitMQConnection
        {
            IConnection GetConnection();
        }

        public class RabbitMQConnection : IRabbitMQConnection, IDisposable
        {
            private readonly IConnection _connection;

            public RabbitMQConnection(string hostname, string username, string password)
            {
                var factory = new ConnectionFactory
                {
                    HostName = hostname,
                    UserName = username,
                    Password = password,
                    DispatchConsumersAsync = true
                };
                _connection = factory.CreateConnection();
            }

            public IConnection GetConnection() => _connection;

            public void Dispose()
            {
                _connection?.Dispose();
            }
        }

        public interface IEventBus
        {
            Task PublishAsync<T>(string exchangeName, string routingKey, T message) where T : class;
        }

        public class RabbitMQEventBus : IEventBus
        {
            private readonly IRabbitMQConnection _connection;
            private readonly ILogger<RabbitMQEventBus> _logger;

            public RabbitMQEventBus(IRabbitMQConnection connection, ILogger<RabbitMQEventBus> logger)
            {
                _connection = connection;
                _logger = logger;
            }

            public async Task PublishAsync<T>(string exchangeName, string routingKey, T message) where T : class
            {
                try
                {
                    using var channel = _connection.GetConnection().CreateModel();

                    channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

                    var json = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    channel.BasicPublish(
                        exchange: exchangeName,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Published event {EventType} to {Exchange} with routing key {RoutingKey}",
                        typeof(T).Name, exchangeName, routingKey);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing event {EventType}", typeof(T).Name);
                    throw;
                }
            }
        }
    }

