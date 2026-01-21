using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.Feature.DeleteProduct;
using ProductService.Feature.GetProduct;
using ProductService.Feature.ListProducts;
using ProductService.Feature.UpdateProduct;
using ProductService.Feature.UpdateStock;
using ProductService.Features.CreateProduct;

namespace ProductService.API.Controllers;

/// <summary>
/// Controller for managing product operations including CRUD operations and stock management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance for sending commands and queries.</param>
    /// <param name="logger">The logger instance for logging controller operations.</param>
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new product in the system.
    /// </summary>
    /// <param name="command">The command containing product details to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created product details with HTTP 201 status.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="409">Product with the same SKU already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProductResponse>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetProduct),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The product details if found.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProductResponse>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetProductQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists products with support for filtering, sorting, and pagination.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10, max: 100).</param>
    /// <param name="category">Optional filter by product category.</param>
    /// <param name="isActive">Optional filter by active status.</param>
    /// <param name="searchTerm">Optional search term to filter by name, description, or SKU.</param>
    /// <param name="sortBy">Field to sort by (Name, Price, StockQuantity, CreatedAt, Category).</param>
    /// <param name="sortDescending">Whether to sort in descending order.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Paginated list of products matching the criteria.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ListProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListProductResponse>> ListProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "Name",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductQuery(
            pageNumber,
            pageSize,
            category,
            isActive,
            searchTerm,
            sortBy,
            sortDescending);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing product's details.
    /// </summary>
    /// <param name="id">The unique identifier of the product to update.</param>
    /// <param name="request">The request containing updated product details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated product details.</returns>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">Concurrency conflict - product was modified by another user.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateProductResponse>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.StockQuantity,
                request.Category,
                request.UpdatedBy);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a product (performs soft delete by deactivating).
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete.</param>
    /// <param name="deletedBy">The identifier of the user performing the deletion.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Product deleted successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        [FromQuery] string deletedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteProductCommand(id, deletedBy), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates the stock quantity of a product.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="request">The request containing stock change details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Details of the stock update including previous and new quantities.</returns>
    /// <response code="200">Stock updated successfully.</response>
    /// <response code="400">Invalid request or insufficient stock.</response>
    /// <response code="404">Product not found.</response>
    [HttpPatch("{id}/stock")]
    [ProducesResponseType(typeof(UpdateStockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateStockResponse>> UpdateStock(
        Guid id,
        [FromBody] UpdateStockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateStockCommand(
                id,
                request.QuantityChange,
                request.Reason,
                request.UpdatedBy);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
