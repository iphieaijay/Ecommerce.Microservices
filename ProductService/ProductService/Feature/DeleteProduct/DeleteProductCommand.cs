using MediatR;

namespace ProductService.Feature.DeleteProduct
{
    public record DeleteProductCommand(Guid Id, string DeletedBy) : IRequest<bool>;
}
