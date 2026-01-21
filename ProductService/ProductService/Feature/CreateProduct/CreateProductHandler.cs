using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Feature.CreateProduct;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Event;
using ProductService.Feature.GetProduct;

namespace ProductService.Features.CreateProduct;
public class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResponse>
{
    private readonly ProductDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(ProductDbContext context, IEventBus eventBus, ILogger<CreateProductHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate SKU
        var existingProduct = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SKU == request.SKU, cancellationToken);

        if (existingProduct != null)
        {
            _logger.LogWarning("Attempted to create product with duplicate SKU: {SKU}", request.SKU);
            throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists");
        }

        // Create product entity
        var product = Product.Create(
            request.Name,
            request.Description,
            request.SKU,
            request.Price,
            request.StockQuantity,
            request.Category,
            request.CreatedBy);

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

            // Publish domain event
            await _eventBus.PublishAsync(new ProductCreatedEvent(
                product.Id,
                product.Name,
                product.SKU,
                product.Price,
                product.StockQuantity,
                product.Category,
                product.CreatedAt
            ), cancellationToken);

            return new CreateProductResponse(
                product.Id,
                product.Name,
                product.SKU,
                product.Price,
                product.StockQuantity,
                product.Category,
                product.CreatedAt);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating product");
            throw new InvalidOperationException("Failed to create product due to database error", ex);
        }
    }
}

