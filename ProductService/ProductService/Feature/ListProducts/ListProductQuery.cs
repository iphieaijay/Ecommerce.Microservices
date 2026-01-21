using MediatR;

namespace ProductService.Feature.ListProducts
{
    public record ListProductQuery(
          int PageNumber = 1,
          int PageSize = 10,
          string? Category = null,
          bool? IsActive = null,
          string? SearchTerm = null,
          string SortBy = "Name",
          bool SortDescending = false
      ) : IRequest<ListProductResponse>;
}
