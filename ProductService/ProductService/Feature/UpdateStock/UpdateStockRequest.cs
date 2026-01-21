namespace ProductService.Feature.UpdateStock
{
    /// <summary>
    /// Request model for updating product stock.
    /// </summary>
    public record UpdateStockRequest(int QuantityChange,string Reason,string UpdatedBy);
}
