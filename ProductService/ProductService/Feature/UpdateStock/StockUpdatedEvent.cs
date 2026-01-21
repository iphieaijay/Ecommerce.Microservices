using ProductService.Infrastructure.Event;

namespace ProductService.Feature.UpdateStock
{
    public record StockUpdatedEvent(
        Guid ProductId,
        string SKU,
        int PreviousQuantity,
        int NewQuantity,
        int QuantityChange,
        string Reason,
        DateTime UpdatedAt
    ) : IEvent;

}
