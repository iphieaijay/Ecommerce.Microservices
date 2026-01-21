using MediatR;

namespace ProductService.Feature.GetProduct
{
    // Query
    public record GetProductQuery(Guid Id) : IRequest<GetProductResponse>;



}
