namespace ProductService.Feature.GetProduct
{
    // Response
    public record GetProductResponse(
        Guid Id,
        string Name,
        string Description,
        string SKU,
        decimal Price,
        int StockQuantity,
        string Category,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

}
