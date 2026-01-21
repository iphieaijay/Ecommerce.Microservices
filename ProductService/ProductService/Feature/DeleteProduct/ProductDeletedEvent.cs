using ProductService.Infrastructure.Event;

namespace ProductService.Feature.DeleteProduct
{
    // Domain Event
    public record ProductDeletedEvent(Guid ProductId,string SKU, DateTime DeletedAt) : IEvent;
}
