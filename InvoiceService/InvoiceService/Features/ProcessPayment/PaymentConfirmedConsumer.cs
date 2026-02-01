using InvoiceService.Domain.Entities;
using InvoiceService.Domain.Events;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MassTransit;
using MediatR;

namespace InvoiceService.Features.ProcessPayment;

// Consumer for PaymentConfirmed Event
public class PaymentConfirmedConsumer : IConsumer<PaymentConfirmedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentConfirmedConsumer> _logger;

    public PaymentConfirmedConsumer(IMediator mediator, ILogger<PaymentConfirmedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentConfirmedEvent> context)
    {
        var paymentEvent = context.Message;

        _logger.LogInformation(
            "Received PaymentConfirmedEvent for OrderId: {OrderId}, PaymentId: {PaymentId}",
            paymentEvent.OrderId, paymentEvent.PaymentId);

        try
        {
            var command = new CreateInvoiceCommand
            {
                OrderId = paymentEvent.OrderId,
                PaymentId = paymentEvent.PaymentId,
                CustomerId = paymentEvent.CustomerId,
                CustomerName = paymentEvent.CustomerInfo.Name,
                CustomerEmail = paymentEvent.CustomerInfo.Email,
                BillingAddress = new CreateInvoiceCommand.AddressDto
                {
                    Street = paymentEvent.CustomerInfo.BillingAddress.Street,
                    City = paymentEvent.CustomerInfo.BillingAddress.City,
                    State = paymentEvent.CustomerInfo.BillingAddress.State,
                    ZipCode = paymentEvent.CustomerInfo.BillingAddress.ZipCode,
                    Country = paymentEvent.CustomerInfo.BillingAddress.Country
                },
                ShippingAddress = paymentEvent.CustomerInfo.ShippingAddress != null
                    ? new CreateInvoiceCommand.AddressDto
                    {
                        Street = paymentEvent.CustomerInfo.ShippingAddress.Street,
                        City = paymentEvent.CustomerInfo.ShippingAddress.City,
                        State = paymentEvent.CustomerInfo.ShippingAddress.State,
                        ZipCode = paymentEvent.CustomerInfo.ShippingAddress.ZipCode,
                        Country = paymentEvent.CustomerInfo.ShippingAddress.Country
                    }
                    : null,
                PaymentMethod = paymentEvent.PaymentMethod,
                PaymentTransactionId = paymentEvent.TransactionId,
                Currency = paymentEvent.Currency,
                OrderItems = paymentEvent.OrderItems.Select(item => new CreateInvoiceCommand.OrderItemDto
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

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully created invoice {InvoiceNumber} for OrderId: {OrderId}",
                    result.InvoiceNumber, paymentEvent.OrderId);
            }
            else
            {
                _logger.LogError(
                    "Failed to create invoice for OrderId: {OrderId}. Error: {Error}",
                    paymentEvent.OrderId, result.Error);

                throw new Exception(result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing PaymentConfirmedEvent for OrderId: {OrderId}",
                paymentEvent.OrderId);

            throw; // Let MassTransit handle retry
        }
    }
}

