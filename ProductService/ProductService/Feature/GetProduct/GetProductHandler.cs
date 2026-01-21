using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Feature.GetProduct;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Feature
{
    public class GetProductHandler : IRequestHandler<GetProductQuery, GetProductResponse>
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<GetProductHandler> _logger;

        public GetProductHandler(ProductDbContext context, ILogger<GetProductHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GetProductResponse> Handle(GetProductQuery request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", request.Id);
                throw new KeyNotFoundException($"Product with ID '{request.Id}' not found");
            }

            _logger.LogInformation("Product retrieved successfully: {ProductId}", product.Id);

            return new GetProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.SKU,
                product.Price,
                product.StockQuantity,
                product.Category,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt);
        }
    }
}
