namespace ProductService.Feature.UpdateStock
{
  
    public record UpdateStockResponse(
        Guid ProductId,
        int PreviousQuantity,
        int NewQuantity,
        int QuantityChange
    );

}
