namespace OrderService.Features.Orders.CreateOrder.DTO
{
    public class CreateOrderRequest
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
