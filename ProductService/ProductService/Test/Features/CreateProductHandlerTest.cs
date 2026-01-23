using MediatR;

namespace ProductService.Test.Features
{
    using global::ProductService.Features.CreateProduct;
    using global::ProductService.Infrastructure.Event.EventBus;
    using global::ProductService.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;
    using ProductService.Domain.Entities;
    using ProductService.Feature.CreateProduct;
    using ProductService.Feature.GetProduct;
    using Xunit;

   
    public class CreateProductHandlerTest : IDisposable
    {
        private readonly ProductDbContext _context;
        private readonly Mock<IEventBus> _eventBusMock;
        private readonly Mock<ILogger<CreateProductHandler>> _loggerMock;
        private readonly CreateProductHandler _handler;

        public CreateProductHandlerTest()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ProductDbContext(options);
            _eventBusMock = new Mock<IEventBus>();
            _loggerMock = new Mock<ILogger<CreateProductHandler>>();

            _handler = new CreateProductHandler(_context, _eventBusMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesProductSuccessfully()
        {
            // Arrange
            var command = new CreateProductCommand(
                Name: "Test Product",
                Description: "Test Description",
                SKU: "TEST-001",
                Price: 99.99m,
                StockQuantity: 100,
                Category: "Electronics",
                CreatedBy: "test-user"
            );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal("TEST-001", result.SKU);
            Assert.Equal(99.99m, result.Price);
            Assert.Equal(100, result.StockQuantity);

            var productInDb = await _context.Products.FindAsync(result.Id);
            Assert.NotNull(productInDb);
            Assert.True(productInDb.IsActive);

            _eventBusMock.Verify(
                x => x.PublishAsync(It.IsAny<ProductCreatedEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateSKU_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingProduct = Product.Create(
                "Existing Product",
                "Description",
                "DUPLICATE-SKU",
                50.00m,
                10,
                "Category",
                "user");

            _context.Products.Add(existingProduct);
            await _context.SaveChangesAsync();

            var command = new CreateProductCommand(
                Name: "New Product",
                Description: "Description",
                SKU: "DUPLICATE-SKU",
                Price: 75.00m,
                StockQuantity: 20,
                Category: "Category",
                CreatedBy: "test-user"
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));

            Assert.Contains("already exists", exception.Message);

            _eventBusMock.Verify(
                x => x.PublishAsync(It.IsAny<ProductCreatedEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(-10.00, 100)] // Negative price
        [InlineData(50.00, -5)]   // Negative stock
        public async Task Handle_InvalidValues_ThrowsArgumentException(decimal price, int stock)
        {
            // Arrange
            var command = new CreateProductCommand(
                Name: "Test Product",
                Description: "Description",
                SKU: "TEST-SKU",
                Price: price,
                StockQuantity: stock,
                Category: "Category",
                CreatedBy: "test-user"
            );

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var command = new CreateProductCommand(
                Name: "",
                Description: "Description",
                SKU: "TEST-SKU",
                Price: 50.00m,
                StockQuantity: 10,
                Category: "Category",
                CreatedBy: "test-user"
            );

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NullDescription_CreatesProductWithEmptyDescription()
        {
            // Arrange
            var command = new CreateProductCommand(
                Name: "Test Product",
                Description: null!,
                SKU: "TEST-002",
                Price: 99.99m,
                StockQuantity: 100,
                Category: "Electronics",
                CreatedBy: "test-user"
            );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var productInDb = await _context.Products.FindAsync(result.Id);
            Assert.NotNull(productInDb);
            Assert.Equal(string.Empty, productInDb.Description);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
