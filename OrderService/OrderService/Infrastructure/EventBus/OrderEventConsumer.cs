using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OrderService.Domain.Entities;
using OrderService.Domain.Enum;
using OrderService.Infrastructure;
using OrderService.Infrastructure.EventBus;
using OrderService.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderService.Infrastructure.EventBus
{
    public class OrderEventConsumer : BackgroundService
    {
        private readonly IRabbitMQConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderEventConsumer> _logger;
        private IModel? _channel;

        public OrderEventConsumer(
            IRabbitMQConnection connection,
            IServiceProvider serviceProvider,
            ILogger<OrderEventConsumer> logger)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = _connection.GetConnection().CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(exchange: "inventory.exchange", type: ExchangeType.Topic, durable: true);

            // Declare queue for order service
            var queueName = _channel.QueueDeclare(
                queue: "order.inventory.events",
                durable: true,
                exclusive: false,
                autoDelete: false).QueueName;

            // Bind to inventory events
            _channel.QueueBind(
                queue: queueName,
                exchange: "inventory.exchange",
                routingKey: "inventory.reserved");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received inventory event: {Message}", message);

                    if (ea.RoutingKey == "inventory.reserved")
                    {
                        await HandleInventoryReservedEvent(message);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            await Task.CompletedTask;
        }

        private async Task HandleInventoryReservedEvent(string message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var inventoryEvent = JsonSerializer.Deserialize<InventoryReservedEvent>(message);
            if (inventoryEvent == null) return;

            var order = await dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == inventoryEvent.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for inventory reserved event", inventoryEvent.OrderId);
                return;
            }

            order.Status = OrderStatus.Reserved;
            order.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked as reserved", order.Id);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }

    public class InventoryReservedEvent
    {
        public Guid OrderId { get; set; }
        public bool Success { get; set; }
        public List<string> ReservedProducts { get; set; } = new();
    }
}