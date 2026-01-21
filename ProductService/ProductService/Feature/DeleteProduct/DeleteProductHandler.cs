using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Event;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Feature.DeleteProduct
{
    public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly ProductDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DeleteProductHandler> _logger;

        public DeleteProductHandler(ProductDbContext context, IEventBus eventBus,ILogger<DeleteProductHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found for deletion: {ProductId}", request.Id);
                throw new KeyNotFoundException($"Product with ID '{request.Id}' not found");
            }

            // Soft delete - deactivate instead of removing
            product.Deactivate(request.DeletedBy);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Product deleted (deactivated): {ProductId}", product.Id);

                // Publish domain event
                await _eventBus.PublishAsync(new ProductDeletedEvent(
                    product.Id,
                    product.SKU,
                    DateTime.UtcNow
                ), cancellationToken);

                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting product: {ProductId}", request.Id);
                throw new InvalidOperationException("Failed to delete product due to database error", ex);
            }
        }
    }

   
}
