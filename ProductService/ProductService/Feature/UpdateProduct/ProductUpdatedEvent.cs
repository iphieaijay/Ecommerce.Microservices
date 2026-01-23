using ProductService.Infrastructure.Event.EventBus;

namespace ProductService.Feature.UpdateProduct
{
    // Domain Event
    public record ProductUpdatedEvent(
        Guid ProductId,
        string Name,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt
    ) : IEvent;
}
