using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Event;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Feature.UpdateStock
{
    // Handler
    public class UpdateStockHandler : IRequestHandler<UpdateStockCommand, UpdateStockResponse>
    {
        private readonly ProductDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger<UpdateStockHandler> _logger;

        public UpdateStockHandler(
            ProductDbContext context,
            IEventBus eventBus,
            ILogger<UpdateStockHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UpdateStockResponse> Handle(
            UpdateStockCommand request,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found for stock update: {ProductId}", request.ProductId);
                throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found");
            }

            if (!product.IsActive)
            {
                _logger.LogWarning("Attempted to update stock for inactive product: {ProductId}", request.ProductId);
                throw new InvalidOperationException("Cannot update stock for inactive product");
            }

            var previousQuantity = product.StockQuantity;

            try
            {
                product.UpdateStock(request.QuantityChange);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Stock updated for product {ProductId}: {PreviousQuantity} -> {NewQuantity} (Change: {Change})",
                    product.Id,
                    previousQuantity,
                    product.StockQuantity,
                    request.QuantityChange);

                // Publish domain event
                await _eventBus.PublishAsync(new StockUpdatedEvent(
                    product.Id,
                    product.SKU,
                    previousQuantity,
                    product.StockQuantity,
                    request.QuantityChange,
                    request.Reason,
                    DateTime.UtcNow
                ), cancellationToken);

                // Check for low stock
                if (product.StockQuantity < 10 && product.StockQuantity >= 0)
                {
                    await _eventBus.PublishAsync(new LowStockWarningEvent(
                        product.Id,
                        product.SKU,
                        product.Name,
                        product.StockQuantity,
                        DateTime.UtcNow
                    ), cancellationToken);
                }

                return new UpdateStockResponse(
                    product.Id,
                    previousQuantity,
                    product.StockQuantity,
                    request.QuantityChange);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient stock"))
            {
                _logger.LogWarning(
                    "Insufficient stock for product {ProductId}: Current {Current}, Attempted change {Change}",
                    request.ProductId,
                    previousQuantity,
                    request.QuantityChange);
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while updating stock: {ProductId}", request.ProductId);
                throw new InvalidOperationException("Stock was modified by another user. Please refresh and try again", ex);
            }
        }
    }
}
