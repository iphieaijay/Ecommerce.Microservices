using MediatR;

namespace ProductService.Feature.UpdateProduct
{
    // Command
    public record UpdateProductCommand(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string UpdatedBy
    ) : IRequest<UpdateProductResponse>;
}
