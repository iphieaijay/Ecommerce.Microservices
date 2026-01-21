namespace ProductService.Feature.GetProduct
{
    public record CreateProductResponse(
    Guid Id,
    string Name,
    string SKU,
    decimal Price,
    int StockQuantity,
    string Category,
    DateTime CreatedAt
);
}
