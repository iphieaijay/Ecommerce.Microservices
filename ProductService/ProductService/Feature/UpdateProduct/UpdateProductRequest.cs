namespace ProductService.Feature.UpdateProduct
{
    /// <summary>
    /// Request model for updating a product.
    /// </summary>

    public record UpdateProductRequest(string Name,string Description,decimal Price,int StockQuantity,
        string Category, string UpdatedBy );

}
