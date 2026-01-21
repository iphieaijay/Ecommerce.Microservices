using MediatR;

namespace ProductService.Feature.UpdateStock
{
    
    public record UpdateStockCommand(
        Guid ProductId,
        int QuantityChange,
        string Reason,
        string UpdatedBy
    ) : IRequest<UpdateStockResponse>;

    
}
