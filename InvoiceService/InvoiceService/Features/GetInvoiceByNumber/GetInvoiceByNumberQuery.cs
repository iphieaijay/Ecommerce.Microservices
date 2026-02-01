using InvoiceService.Features.GetInvoice;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MediatR;

namespace InvoiceService.Features.GetInvoiceByNumber;

// ---------------------------------------------------------------------------
// Query
// ---------------------------------------------------------------------------

public class GetInvoiceByNumberQuery : IRequest<InvoiceDto?>
{
    public string InvoiceNumber { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public class GetInvoiceByNumberQueryHandler : IRequestHandler<GetInvoiceByNumberQuery, InvoiceDto?>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<GetInvoiceByNumberQueryHandler> _logger;

    public GetInvoiceByNumberQueryHandler(
        IInvoiceRepository repository,
        ILogger<GetInvoiceByNumberQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<InvoiceDto?> Handle(
        GetInvoiceByNumberQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByInvoiceNumberAsync(
            request.InvoiceNumber, cancellationToken);

        if (invoice is null)
        {
            _logger.LogWarning(
                "GetInvoiceByNumberQuery — no invoice found for number: {InvoiceNumber}",
                request.InvoiceNumber);
            return null;
        }

        return new InvoiceDto
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
    }
}