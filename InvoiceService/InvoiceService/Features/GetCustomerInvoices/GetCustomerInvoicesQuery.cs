using InvoiceService.Features.GetInvoice;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Features.GetCustomerInvoices;

// Query
public class GetCustomerInvoicesQuery : IRequest<List<InvoiceDto>>
{
    public Guid CustomerId { get; set; }
}

// Handler
public class GetCustomerInvoicesQueryHandler : IRequestHandler<GetCustomerInvoicesQuery, List<InvoiceDto>>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<GetCustomerInvoicesQueryHandler> _logger;

    public GetCustomerInvoicesQueryHandler(IInvoiceRepository repository, ILogger<GetCustomerInvoicesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<InvoiceDto>> Handle(GetCustomerInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        _logger.LogInformation("Retrieved {Count} invoices for CustomerId: {CustomerId}",
            invoices.Count, request.CustomerId);

        return invoices.Select(invoice => new InvoiceDto
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
            PaymentMethod = invoice.PaymentMethod,
            PaidDate = invoice.PaidDate,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        }).ToList();
    }
}

// Endpoint
public static class GetCustomerInvoicesEndpoint
{
    public static IEndpointRouteBuilder MapGetCustomerInvoicesEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/customers/{customerId:guid}/invoices", async (
            [FromRoute] Guid customerId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCustomerInvoicesQuery { CustomerId = customerId };
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetCustomerInvoices")
        .WithTags("Invoices")
        .Produces<List<InvoiceDto>>(200);

        return builder;
    }
}
