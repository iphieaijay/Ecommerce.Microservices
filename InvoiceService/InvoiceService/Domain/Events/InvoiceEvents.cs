namespace InvoiceService.Domain.Events;

// Incoming Event from Payment Service
public record PaymentConfirmedEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public PaymentCustomerInfo CustomerInfo { get; init; } = null!;
    public List<PaymentOrderItem> OrderItems { get; init; } = new();
}

public record PaymentCustomerInfo
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public PaymentAddress BillingAddress { get; init; } = null!;
    public PaymentAddress? ShippingAddress { get; init; }
}

public record PaymentAddress
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record PaymentOrderItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSku { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public decimal DiscountPercentage { get; init; }
}

// Outgoing Events from Invoice Service
public record InvoiceCreatedEvent
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime InvoiceDate { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
}

public record InvoiceIssuedEvent
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime IssuedDate { get; init; }
}

public record InvoiceFailedEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedDate { get; init; }
    public int RetryCount { get; init; }
}
