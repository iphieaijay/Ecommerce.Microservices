using InvoiceService.Features.CancelInvoice;
using InvoiceService.Features.GetCustomerInvoices;
using InvoiceService.Features.GetInvoice;
using InvoiceService.Features.GetInvoiceByNumber;
using InvoiceService.Features.GetInvoiceByOrderId;
using InvoiceService.Features.ProcessPayment;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace InvoiceService.Controllers
{
    [Route("api")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IMediator mediator, ILogger<InvoiceController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // POST /api/invoices
        // ---------------------------------------------------------------
        /// <summary>
        /// Creates a new invoice manually.  In normal e-commerce flow the invoice
        /// is created automatically when a <c>PaymentConfirmedEvent</c> is consumed
        /// from RabbitMQ.  This endpoint exists for cases where an invoice must be
        /// raised outside that event (back-office correction, manual order, etc.).
        /// </summary>
        /// <param name="request">Full invoice payload including customer, address, and line-item details.</param>
        /// <returns>
        /// 201 – invoice created; the <c>Location</c> header points to the new resource.
        /// 400 – one or more validation rules failed; the response body lists every error.
        /// 409 – an invoice for the same <c>OrderId</c> already exists (idempotency guard).
        /// </returns>
        /// <response code="201">Invoice created successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="409">An invoice already exists for the given OrderId.</response>
        [HttpPost("invoices")]
        [ProducesResponseType(typeof(CreateInvoiceResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateInvoice(
            [FromBody] CreateInvoiceRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "CreateInvoice called for OrderId: {OrderId}, CustomerId: {CustomerId}",
                request.OrderId, request.CustomerId);

            // Map HTTP DTO → MediatR command
            var command = new CreateInvoiceCommand
            {
                OrderId = request.OrderId,
                PaymentId = request.PaymentId,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                BillingAddress = new CreateInvoiceCommand.AddressDto
                {
                    Street = request.BillingAddress.Street,
                    City = request.BillingAddress.City,
                    State = request.BillingAddress.State,
                    ZipCode = request.BillingAddress.ZipCode,
                    Country = request.BillingAddress.Country
                },
                ShippingAddress = request.ShippingAddress != null
                    ? new CreateInvoiceCommand.AddressDto
                    {
                        Street = request.ShippingAddress.Street,
                        City = request.ShippingAddress.City,
                        State = request.ShippingAddress.State,
                        ZipCode = request.ShippingAddress.ZipCode,
                        Country = request.ShippingAddress.Country
                    }
                    : null,
                PaymentMethod = request.PaymentMethod,
                PaymentTransactionId = request.PaymentTransactionId,
                Currency = request.Currency,
                OrderItems = request.OrderItems.Select(item => new CreateInvoiceCommand.OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductSku = item.ProductSku,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    DiscountPercentage = item.DiscountPercentage
                }).ToList()
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                // The handler returns a failure string that starts with
                // "Validation failed:" when FluentValidation rejects the payload.
                // Every other failure string is a business-rule violation.
                if (result.Error!.StartsWith("Validation failed:"))
                    return BadRequest(new ErrorResponse("Validation failed", result.Error));

                // Duplicate invoice detected by the idempotency guard in the handler.
                return Conflict(new ErrorResponse("Duplicate invoice",
                    $"An invoice already exists for OrderId '{request.OrderId}'. " +
                    $"Existing InvoiceId: {result.InvoiceId}, Number: {result.InvoiceNumber}"));
            }

            // 201 Created + Location header pointing to the newly created resource
            return CreatedAtAction(
                actionName: nameof(GetInvoiceById),
                routeValues: new { id = result.InvoiceId },
                value: result);
        }


        // ---------------------------------------------------------------
        // GET  /api/invoices/{id}
        // ---------------------------------------------------------------
        /// <summary>
        /// Retrieves a single invoice by its unique identifier.
        /// </summary>
        /// <param name="id">Invoice GUID.</param>
        /// <returns>The invoice, or 404 when it does not exist.</returns>
        /// <response code="200">Invoice returned successfully.</response>
        /// <response code="400">The supplied id is not a valid GUID.</response>
        /// <response code="404">No invoice matches the given id.</response>
        [HttpGet("invoices/{id:guid}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetInvoiceById called with InvoiceId: {InvoiceId}", id);

            var result = await _mediator.Send(new GetInvoiceQuery { InvoiceId = id }, cancellationToken);

            if (result is null)
                return NotFound(new ErrorResponse("Invoice not found", $"No invoice exists with id '{id}'."));

            return Ok(result);
        }

        // ---------------------------------------------------------------
        // GET  /api/invoices/number/{invoiceNumber}
        // ---------------------------------------------------------------
        /// <summary>
        /// Retrieves a single invoice by its human-readable invoice number (e.g. INV-20250131-4821).
        /// </summary>
        /// <param name="invoiceNumber">The invoice number string.</param>
        /// <returns>The invoice, or 404 when it does not exist.</returns>
        /// <response code="200">Invoice returned successfully.</response>
        /// <response code="400">The invoice number is empty or missing.</response>
        /// <response code="404">No invoice matches the given number.</response>
        [HttpGet("invoices/number/{invoiceNumber}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceByNumber(
            [FromRoute][StringLength(50, MinimumLength = 1)] string invoiceNumber,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetInvoiceByNumber called with InvoiceNumber: {InvoiceNumber}", invoiceNumber);

            var result = await _mediator.Send(
                new GetInvoiceByNumberQuery { InvoiceNumber = invoiceNumber }, cancellationToken);

            if (result is null)
                return NotFound(new ErrorResponse("Invoice not found",
                    $"No invoice exists with number '{invoiceNumber}'."));

            return Ok(result);
        }

        // ---------------------------------------------------------------
        // GET  /api/invoices/order/{orderId}
        // ---------------------------------------------------------------
        /// <summary>
        /// Retrieves the invoice that was generated for a specific order.
        /// Each confirmed order produces exactly one invoice.
        /// </summary>
        /// <param name="orderId">Order GUID.</param>
        /// <returns>The invoice, or 404 when none has been created for that order yet.</returns>
        /// <response code="200">Invoice returned successfully.</response>
        /// <response code="400">The supplied orderId is not a valid GUID.</response>
        /// <response code="404">No invoice has been generated for the given order.</response>
        [HttpGet("invoices/order/{orderId:guid}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceByOrderId(
            [FromRoute] Guid orderId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetInvoiceByOrderId called with OrderId: {OrderId}", orderId);

            var result = await _mediator.Send(new GetInvoiceByOrderIdQuery { OrderId = orderId }, cancellationToken);

            if (result is null)
                return NotFound(new ErrorResponse("Invoice not found", $"No invoice exists for order '{orderId}'."));

            return Ok(result);
        }

        // ---------------------------------------------------------------
        // GET  /api/customers/{customerId}/invoices
        // ---------------------------------------------------------------
        /// <summary>
        /// Returns every invoice that belongs to a given customer, ordered newest-first.
        /// An empty array is a valid response when the customer has no invoices yet.
        /// </summary>
        /// <param name="customerId">Customer GUID.</param>
        /// <returns>Array of invoices (may be empty).</returns>
        /// <response code="200">Invoice list returned successfully (may be empty).</response>
        /// <response code="400">The supplied customerId is not a valid GUID.</response>
        [HttpGet("customers/{customerId:guid}/invoices")]
        [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInvoicesByCustomerId(
            [FromRoute] Guid customerId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetInvoicesByCustomerId called with CustomerId: {CustomerId}", customerId);

            var result = await _mediator.Send(
                new GetCustomerInvoicesQuery { CustomerId = customerId }, cancellationToken);

            return Ok(result);
        }

        // ---------------------------------------------------------------
        // PUT  /api/invoices/{id}/cancel
        // ---------------------------------------------------------------
        /// <summary>
        /// Cancels an invoice that has not yet been paid.
        /// A cancellation reason is mandatory so that every cancellation is auditable.
        /// Attempting to cancel an already-paid invoice returns 409 Conflict.
        /// </summary>
        /// <param name="id">Invoice GUID to cancel.</param>
        /// <param name="request">Body carrying the cancellation reason.</param>
        /// <returns>The updated invoice after cancellation.</returns>
        /// <response code="200">Invoice cancelled successfully.</response>
        /// <response code="400">Validation failed (missing or empty reason).</response>
        /// <response code="404">No invoice matches the given id.</response>
        /// <response code="409">The invoice is already paid and cannot be cancelled.</response>
        [HttpPut("invoices/{id:guid}/cancel")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelInvoice([FromRoute] Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CancelInvoice called for InvoiceId: {InvoiceId}", id);

            var command = new CancelInvoiceCommand
            {
                InvoiceId = id,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.FailureKind == CancelInvoiceFailureKind.NotFound)
                    return NotFound(new ErrorResponse("Invoice not found", result.Error!));

                if (result.FailureKind == CancelInvoiceFailureKind.AlreadyPaid)
                    return Conflict(new ErrorResponse("Cannot cancel invoice", result.Error!));

                return BadRequest(new ErrorResponse("Cancellation failed", result.Error!));
            }

            return Ok(result.Invoice);
        }

        // ---------------------------------------------------------------
        // GET  /api/invoices/health
        // ---------------------------------------------------------------
        /// <summary>
        /// Lightweight liveness probe that returns the current service timestamp.
        /// Use the dedicated /health endpoint for full dependency checks (DB + RabbitMQ).
        /// </summary>
        /// <returns>A JSON object with service metadata.</returns>
        /// <response code="200">Always succeeds while the process is alive.</response>
        [HttpGet("invoices/health")]
        [ProducesResponseType(typeof(ServiceInfoResponse), StatusCodes.Status200OK)]
        public IActionResult GetServiceInfo()
        {
            return Ok(new ServiceInfoResponse
            {
                Service = "Invoice Service",
                Version = "1.0.0",
                Status = "Running",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    // ---------------------------------------------------------------------------
    // Shared request / response DTOs that live alongside the controller because
    // they are purely an HTTP-layer concern — they have no business logic and are
    // not referenced anywhere else in the application.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Body sent to the cancel endpoint.
    /// </summary>
    public class CancelInvoiceRequest
    {
        /// <summary>
        /// Human-readable reason for the cancellation.  Stored in the invoice's
        /// Notes field so every cancellation is auditable.
        /// </summary>
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Reason must be between 1 and 1000 characters")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Standard error envelope returned on 4xx responses so clients always get
    /// a consistent shape regardless of which endpoint raised the error.
    /// </summary>
    public class ErrorResponse
    {
        public string Title { get; set; }
        public string Detail { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ErrorResponse(string title, string detail)
        {
            Title = title;
            Detail = detail;
        }
    }
    public class CreateInvoiceRequest
    {
        [Required] public Guid OrderId { get; set; }
        [Required] public Guid PaymentId { get; set; }
        [Required] public Guid CustomerId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required] public AddressRequest BillingAddress { get; set; } = null!;

        /// <summary>Omit entirely or set to <c>null</c> to default to the billing address.</summary>
        public AddressRequest? ShippingAddress { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string PaymentTransactionId { get; set; } = string.Empty;

        /// <summary>ISO 4217 three-letter currency code. Defaults to USD.</summary>
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "USD";

        /// <summary>At least one line item is required.</summary>
        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<OrderItemRequest> OrderItems { get; set; } = new();
    }

    /// <summary>Address sub-object used inside <see cref="CreateInvoiceRequest"/>.</summary>
    public class AddressRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>Single line-item sub-object used inside <see cref="CreateInvoiceRequest"/>.</summary>
    public class OrderItemRequest
    {
        [Required] public Guid ProductId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProductSku { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal TaxRate { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
        public decimal DiscountPercentage { get; set; }
    }

    /// <summary>
    /// Returned by the lightweight /api/invoices/health liveness probe.
    /// </summary>
    public class ServiceInfoResponse
    {
        public string Service { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
