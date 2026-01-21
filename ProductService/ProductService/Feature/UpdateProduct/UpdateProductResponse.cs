namespace ProductService.Feature.UpdateProduct
{
    // Response
    public record UpdateProductResponse(
        Guid Id,
        string Name,
        decimal Price,
        int StockQuantity,
        string Category,
        DateTime UpdatedAt
    );

}
