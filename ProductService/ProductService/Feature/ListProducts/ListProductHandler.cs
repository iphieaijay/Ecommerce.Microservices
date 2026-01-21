using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Feature.ListProducts;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Feature.ListProducts
{
    public class ListProductHandler : IRequestHandler<ListProductQuery, ListProductResponse>
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ListProductHandler> _logger;

        public ListProductHandler(ProductDbContext context, ILogger<ListProductHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ListProductResponse> Handle(ListProductQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Products.AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                query = query.Where(p => p.Category == request.Category);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.SKU.ToLower().Contains(searchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy switch
            {
                "Price" => request.SortDescending
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),
                "StockQuantity" => request.SortDescending
                    ? query.OrderByDescending(p => p.StockQuantity)
                    : query.OrderBy(p => p.StockQuantity),
                "CreatedAt" => request.SortDescending
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                "Category" => request.SortDescending
                    ? query.OrderByDescending(p => p.Category)
                    : query.OrderBy(p => p.Category),
                _ => request.SortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name)
            };

            // Apply pagination
            var products = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.SKU,
                    p.Price,
                    p.StockQuantity,
                    p.Category,
                    p.IsActive))
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogInformation(
                "Retrieved {Count} products (Page {PageNumber}/{TotalPages})",
                products.Count,
                request.PageNumber,
                totalPages);

            return new ListProductResponse(
                products,
                totalCount,
                request.PageNumber,
                request.PageSize,
                totalPages);
        }
    }
}
