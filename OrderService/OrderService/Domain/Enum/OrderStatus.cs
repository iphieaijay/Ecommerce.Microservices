namespace OrderService.Domain.Enum
{
    public enum OrderStatus
    {
        Pending = 0,
        Reserved = 1,
        Confirmed = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5,
        Failed = 6
    }
}
