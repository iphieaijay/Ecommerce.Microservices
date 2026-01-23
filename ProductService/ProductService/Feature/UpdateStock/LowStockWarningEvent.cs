using ProductService.Infrastructure.Event.EventBus;

namespace ProductService.Feature.UpdateStock
{
    public record LowStockWarningEvent(
        Guid ProductId,
        string SKU,
        string ProductName,
        int CurrentStock,
        DateTime Timestamp
    ) : IEvent;
}
