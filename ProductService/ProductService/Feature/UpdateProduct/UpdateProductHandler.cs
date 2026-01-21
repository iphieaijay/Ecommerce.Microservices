using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Event;



namespace ProductService.Feature.UpdateProduct
{
     
    public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResponse>
    {
        private readonly ProductDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger<UpdateProductHandler> _logger;

        public UpdateProductHandler(ProductDbContext context,IEventBus eventBus,ILogger<UpdateProductHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UpdateProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", request.Id);
                throw new KeyNotFoundException($"Product with ID '{request.Id}' not found");
            }

            try
            {
                product.Update(
                    request.Name,
                    request.Description,
                    request.Price,
                    request.StockQuantity,
                    request.Category,
                    request.UpdatedBy);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Product updated successfully: {ProductId}", product.Id);

                // Publish domain event
                await _eventBus.PublishAsync(new ProductUpdatedEvent(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.StockQuantity,
                    product.UpdatedAt ?? DateTime.UtcNow
                ), cancellationToken);

                return new UpdateProductResponse(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.StockQuantity,
                    product.Category,
                    product.UpdatedAt ?? DateTime.UtcNow);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while updating product: {ProductId}", request.Id);
                throw new InvalidOperationException("Product was modified by another user. Please refresh and try again", ex);
            }
        }
    }

    
}
