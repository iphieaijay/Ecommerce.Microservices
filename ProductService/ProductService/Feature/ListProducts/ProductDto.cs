namespace ProductService.Feature.ListProducts
{
    public record ProductDto(
        Guid Id,
        string Name,
        string Description,
        string SKU,
        decimal Price,
        int StockQuantity,
        string Category,
        bool IsActive
    );
}
