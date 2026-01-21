using MediatR;

namespace ProductService.Feature.GetProduct
{
    public record CreateProductCommand(
     string Name,
     string Description,
     string SKU,
     decimal Price,
     int StockQuantity,
     string Category,
     string CreatedBy
 ) : IRequest<CreateProductResponse>;

}
