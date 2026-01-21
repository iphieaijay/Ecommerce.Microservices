using ProductService.Feature.ListProducts;

namespace ProductService.Feature.ListProducts
{
    public record ListProductResponse(
      List<ProductDto> Products,
      int TotalCount,
      int PageNumber,
      int PageSize,
      int TotalPages
  );
}
