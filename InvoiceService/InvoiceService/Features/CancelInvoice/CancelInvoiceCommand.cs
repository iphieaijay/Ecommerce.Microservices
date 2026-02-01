using FluentValidation;
using InvoiceService.Features.GetInvoice;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MediatR;

namespace InvoiceService.Features.CancelInvoice;

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

public class CancelInvoiceCommand : IRequest<CancelInvoiceResult>
{
    public Guid InvoiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Result
// ---------------------------------------------------------------------------

/// <summary>
/// Discriminated failure kind so the controller can map directly to an HTTP
/// status code without having to re-query the database to figure out *why*
/// the cancellation was rejected.
/// </summary>
public enum CancelInvoiceFailureKind
{
    None = 0,
    NotFound = 1,
    AlreadyPaid = 2
}

public class CancelInvoiceResult
{
    public bool IsSuccess { get; private set; }
    public InvoiceDto? Invoice { get; private set; }
    public string? Error { get; private set; }
    public CancelInvoiceFailureKind FailureKind { get; private set; }

    public static CancelInvoiceResult Success(InvoiceDto invoice) => new()
    {
        IsSuccess = true,
        Invoice = invoice,
        FailureKind = CancelInvoiceFailureKind.None
    };

    public static CancelInvoiceResult Failure(string error, CancelInvoiceFailureKind kind) => new()
    {
        IsSuccess = false,
        Error = error,
        FailureKind = kind
    };
}

// ---------------------------------------------------------------------------
// Validator
// ---------------------------------------------------------------------------

public class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty()
            .WithMessage("InvoiceId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A cancellation reason is required.")
            .MaximumLength(1000)
            .WithMessage("Reason must not exceed 1 000 characters.");
    }
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand, CancelInvoiceResult>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<CancelInvoiceCommandHandler> _logger;
    private readonly IValidator<CancelInvoiceCommand> _validator;

    public CancelInvoiceCommandHandler(
        IInvoiceRepository repository,
        ILogger<CancelInvoiceCommandHandler> logger,
        IValidator<CancelInvoiceCommand> validator)
    {
        _repository = repository;
        _logger = logger;
        _validator = validator;
    }

    public async Task<CancelInvoiceResult> Handle(
        CancelInvoiceCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate --------------------------------------------------------
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("CancelInvoiceCommand validation failed: {Errors}", errors);

            // Validation failure is a client mistake — NotFound is the closest
            // semantic match when the id itself is empty/default, but in practice
            // the controller's [Required] attribute on the route will catch an
            // empty GUID before it reaches here.  We use a generic failure kind
            // that the controller treats as 400.
            return CancelInvoiceResult.Failure(errors, CancelInvoiceFailureKind.None);
        }

        // 2. Fetch ------------------------------------------------------------
        var invoice = await _repository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("CancelInvoice — invoice not found: {InvoiceId}", command.InvoiceId);
            return CancelInvoiceResult.Failure(
                $"No invoice exists with id '{command.InvoiceId}'.",
                CancelInvoiceFailureKind.NotFound);
        }

        // 3. Business-rule check ----------------------------------------------
        // MarkAsCancelled throws when Status == Paid.  We check first so we can
        // return a structured result rather than letting an exception bubble.
        if (invoice.Status == InvoiceService.Domain.Entities.InvoiceStatus.Paid)
        {
            _logger.LogWarning(
                "CancelInvoice — invoice {InvoiceNumber} is already paid and cannot be cancelled.",
                invoice.InvoiceNumber);

            return CancelInvoiceResult.Failure(
                $"Invoice '{invoice.InvoiceNumber}' has already been paid and cannot be cancelled.",
                CancelInvoiceFailureKind.AlreadyPaid);
        }

        // 4. Mutate & persist -------------------------------------------------
        invoice.MarkAsCancelled(command.Reason);
        await _repository.UpdateAsync(invoice, cancellationToken);

        _logger.LogInformation(
            "Invoice {InvoiceNumber} cancelled successfully. Reason: {Reason}",
            invoice.InvoiceNumber, command.Reason);

        // 5. Map to DTO & return ----------------------------------------------
        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            OrderId = invoice.OrderId,
            PaymentId = invoice.PaymentId,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status.ToString(),
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            CustomerName = invoice.CustomerName,
            CustomerEmail = invoice.CustomerEmail,
            BillingAddress = invoice.BillingAddress != null
                ? new InvoiceDto.AddressDto
                {
                    Street = invoice.BillingAddress.Street,
                    City = invoice.BillingAddress.City,
                    State = invoice.BillingAddress.State,
                    ZipCode = invoice.BillingAddress.ZipCode,
                    Country = invoice.BillingAddress.Country
                }
                : null,
            ShippingAddress = invoice.ShippingAddress != null
                ? new InvoiceDto.AddressDto
                {
                    Street = invoice.ShippingAddress.Street,
                    City = invoice.ShippingAddress.City,
                    State = invoice.ShippingAddress.State,
                    ZipCode = invoice.ShippingAddress.ZipCode,
                    Country = invoice.ShippingAddress.Country
                }
                : null,
            PaymentMethod = invoice.PaymentMethod,
            PaymentTransactionId = invoice.PaymentTransactionId,
            PaidDate = invoice.PaidDate,
            LineItems = invoice.LineItems.Select(li => new InvoiceDto.LineItemDto
            {
                Id = li.Id,
                ProductId = li.ProductId,
                ProductName = li.ProductName,
                ProductSku = li.ProductSku,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxAmount = li.TaxAmount,
                DiscountAmount = li.DiscountAmount,
                TotalPrice = li.TotalPrice
            }).ToList(),
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };

        return CancelInvoiceResult.Success(dto);
    }
}