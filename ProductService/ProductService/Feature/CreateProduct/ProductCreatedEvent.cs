using ProductService.Infrastructure.Event;

namespace ProductService.Feature.CreateProduct
{
    public record ProductCreatedEvent(
        Guid ProductId,
        string Name,
        string SKU,
        decimal Price,
        int StockQuantity,
        string Category,
        DateTime CreatedAt
    ) : IEvent;

}
