namespace OrderService.Domain.Events
{
    namespace OrderService.Domain.Events
    {
        public abstract class IntegrationEvent
        {
            public Guid EventId { get; set; } = Guid.NewGuid();
            public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        }

        public class OrderCreatedEvent : IntegrationEvent
        {
            public Guid OrderId { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public Guid UserId { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public List<OrderItemDto> Items { get; set; } = new();
            public string ShippingAddress { get; set; } = string.Empty;
        }

        public class OrderReservedEvent : IntegrationEvent
        {
            public Guid OrderId { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public Guid UserId { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public DateTime ReservedAt { get; set; }
            public List<ReservedItemDto> ReservedItems { get; set; } = new();
        }

        public class OrderItemDto
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public class ReservedItemDto
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int ReservedQuantity { get; set; }
            public string InventoryReservationId { get; set; } = string.Empty;
        }
    }
}
