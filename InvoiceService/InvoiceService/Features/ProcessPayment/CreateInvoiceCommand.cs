using FluentValidation;
using InvoiceService.Domain.Entities;
using InvoiceService.Domain.Events;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MassTransit;
using MediatR;

namespace InvoiceService.Features.ProcessPayment;

// Command
public class CreateInvoiceCommand : IRequest<CreateInvoiceResult>
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public AddressDto BillingAddress { get; set; } = null!;
    public AddressDto? ShippingAddress { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentTransactionId { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public List<OrderItemDto> OrderItems { get; set; } = new();

    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}

// Result
public class CreateInvoiceResult
{
    public bool IsSuccess { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? Error { get; set; }

    public static CreateInvoiceResult Success(Guid invoiceId, string invoiceNumber)
        => new() { IsSuccess = true, InvoiceId = invoiceId, InvoiceNumber = invoiceNumber };

    public static CreateInvoiceResult Failure(string error)
        => new() { IsSuccess = false, Error = error };
}

// Validator
public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required");
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("PaymentId is required");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required");
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200).WithMessage("Customer name is required");
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().WithMessage("Valid email is required");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be 3 characters");
        RuleFor(x => x.OrderItems).NotEmpty().WithMessage("Order must have at least one item");

        RuleFor(x => x.BillingAddress).NotNull().WithMessage("Billing address is required");
        RuleFor(x => x.BillingAddress.Street).NotEmpty().When(x => x.BillingAddress != null);
        RuleFor(x => x.BillingAddress.City).NotEmpty().When(x => x.BillingAddress != null);
        RuleFor(x => x.BillingAddress.Country).NotEmpty().When(x => x.BillingAddress != null);

        RuleForEach(x => x.OrderItems).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

// Handler
public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IInvoiceRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateInvoiceCommandHandler> _logger;
    private readonly IValidator<CreateInvoiceCommand> _validator;

    public CreateInvoiceCommandHandler(
        IInvoiceRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateInvoiceCommandHandler> logger,
        IValidator<CreateInvoiceCommand> validator)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _validator = validator;
    }

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate command
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for CreateInvoiceCommand: {Errors}", errors);
                return CreateInvoiceResult.Failure($"Validation failed: {errors}");
            }

            // Check for duplicate invoice
            var existingInvoice = await _repository.ExistsByOrderIdAsync(request.OrderId, cancellationToken);
            if (existingInvoice)
            {
                _logger.LogWarning("Invoice already exists for OrderId: {OrderId}", request.OrderId);
                var existing = await _repository.GetByOrderIdAsync(request.OrderId, cancellationToken);
                return CreateInvoiceResult.Success(existing!.Id, existing.InvoiceNumber);
            }

            // Create line items
            var lineItems = request.OrderItems.Select(item =>
                InvoiceLineItem.Create(
                    item.ProductId,
                    item.ProductName,
                    item.ProductSku,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.TaxRate,
                    item.DiscountPercentage
                )).ToList();

            // Create addresses
            var billingAddress = Address.Create(
                request.BillingAddress.Street,
                request.BillingAddress.City,
                request.BillingAddress.State,
                request.BillingAddress.ZipCode,
                request.BillingAddress.Country
            );

            Address? shippingAddress = null;
            if (request.ShippingAddress != null)
            {
                shippingAddress = Address.Create(
                    request.ShippingAddress.Street,
                    request.ShippingAddress.City,
                    request.ShippingAddress.State,
                    request.ShippingAddress.ZipCode,
                    request.ShippingAddress.Country
                );
            }

            // Create invoice
            var invoice = Invoice.Create(
                request.OrderId,
                request.PaymentId,
                request.CustomerId,
                request.CustomerName,
                request.CustomerEmail,
                billingAddress,
                shippingAddress,
                lineItems,
                request.PaymentMethod,
                request.PaymentTransactionId,
                request.Currency
            );

            // Mark as issued and paid since payment is already confirmed
            invoice.MarkAsIssued();
            invoice.MarkAsPaid();

            // Save to database
            await _repository.AddAsync(invoice, cancellationToken);

            _logger.LogInformation(
                "Invoice {InvoiceNumber} created successfully for OrderId: {OrderId}",
                invoice.InvoiceNumber, request.OrderId);

            // Publish InvoiceCreatedEvent
            await _publishEndpoint.Publish(new InvoiceCreatedEvent
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                OrderId = invoice.OrderId,
                CustomerId = invoice.CustomerId,
                TotalAmount = invoice.TotalAmount,
                Currency = invoice.Currency,
                InvoiceDate = invoice.InvoiceDate,
                CustomerEmail = invoice.CustomerEmail
            }, cancellationToken);

            // Publish InvoiceIssuedEvent
            await _publishEndpoint.Publish(new InvoiceIssuedEvent
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerEmail = invoice.CustomerEmail,
                TotalAmount = invoice.TotalAmount,
                IssuedDate = DateTime.UtcNow
            }, cancellationToken);

            return CreateInvoiceResult.Success(invoice.Id, invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for OrderId: {OrderId}", request.OrderId);

            // Publish failure event
            await _publishEndpoint.Publish(new InvoiceFailedEvent
            {
                PaymentId = request.PaymentId,
                OrderId = request.OrderId,
                Reason = ex.Message,
                FailedDate = DateTime.UtcNow,
                RetryCount = 0
            }, cancellationToken);

            return CreateInvoiceResult.Failure(ex.Message);
        }
    }
}
