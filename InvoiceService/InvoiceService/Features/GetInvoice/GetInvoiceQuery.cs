using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Features.GetInvoice;

// Query
public class GetInvoiceQuery : IRequest<InvoiceDto?>
{
    public Guid InvoiceId { get; set; }
}

// DTO
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentTransactionId { get; set; }
    public DateTime? PaidDate { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class LineItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

// Handler
public class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, InvoiceDto?>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<GetInvoiceQueryHandler> _logger;

    public GetInvoiceQueryHandler(IInvoiceRepository repository, ILogger<GetInvoiceQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<InvoiceDto?> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.InvoiceId, cancellationToken);

        if (invoice == null)
        {
            _logger.LogWarning("Invoice not found: {InvoiceId}", request.InvoiceId);
            return null;
        }

        return MapToDto(invoice);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
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
            BillingAddress = invoice.BillingAddress != null ? new InvoiceDto.AddressDto
            {
                Street = invoice.BillingAddress.Street,
                City = invoice.BillingAddress.City,
                State = invoice.BillingAddress.State,
                ZipCode = invoice.BillingAddress.ZipCode,
                Country = invoice.BillingAddress.Country
            } : null,
            ShippingAddress = invoice.ShippingAddress != null ? new InvoiceDto.AddressDto
            {
                Street = invoice.ShippingAddress.Street,
                City = invoice.ShippingAddress.City,
                State = invoice.ShippingAddress.State,
                ZipCode = invoice.ShippingAddress.ZipCode,
                Country = invoice.ShippingAddress.Country
            } : null,
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

//// Endpoint
//public static class GetInvoiceEndpoint
//{
//    public static IEndpointRouteBuilder MapGetInvoiceEndpoint(this IEndpointRouteBuilder builder)
//    {
//        builder.MapGet("/api/invoices/{id:guid}", async (
//            [FromRoute] Guid id,
//            [FromServices] IMediator mediator,
//            CancellationToken cancellationToken) =>
//        {
//            var query = new GetInvoiceQuery { InvoiceId = id };
//            var result = await mediator.Send(query, cancellationToken);

//            return result != null ? Results.Ok(result) : Results.NotFound();
//        })
//        .WithName("GetInvoice")
//        .WithTags("Invoices")
//        .Produces<InvoiceDto>(200)
//        .Produces(404);

//        return builder;
//    }
//}
