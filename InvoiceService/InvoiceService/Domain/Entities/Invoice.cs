namespace InvoiceService.Domain.Entities;

public class Invoice
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid PaymentId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public Address? BillingAddress { get; private set; }
    public Address? ShippingAddress { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? PaymentTransactionId { get; private set; }
    public DateTime? PaidDate { get; private set; }
    public List<InvoiceLineItem> LineItems { get; private set; } = new();
     public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? Notes { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Invoice() { }

    public static Invoice Create(
        Guid orderId,
        Guid paymentId,
        Guid customerId,
        string customerName,
        string customerEmail,
        Address billingAddress,
        Address? shippingAddress,
        List<InvoiceLineItem> lineItems,
        string paymentMethod,
        string? paymentTransactionId,
        string currency = "USD")
    {
        if (lineItems == null || lineItems.Count == 0)
            throw new InvalidOperationException("Invoice must have at least one line item");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = GenerateInvoiceNumber(),
            OrderId = orderId,
            PaymentId = paymentId,
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            BillingAddress = billingAddress,
            ShippingAddress = shippingAddress ?? billingAddress,
            PaymentMethod = paymentMethod,
            PaymentTransactionId = paymentTransactionId,
            Currency = currency,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            PaidDate = DateTime.UtcNow,
            Status = InvoiceStatus.Draft,
            LineItems = lineItems,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        invoice.CalculateTotals();
        return invoice;
    }

    public void CalculateTotals()
    {
        SubTotal = LineItems.Sum(item => item.TotalPrice);
        TaxAmount = LineItems.Sum(item => item.TaxAmount);
        DiscountAmount = LineItems.Sum(item => item.DiscountAmount);
        TotalAmount = SubTotal + TaxAmount - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsIssued()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot issue invoice in {Status} status");

        Status = InvoiceStatus.Issued;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            return;

        Status = InvoiceStatus.Paid;
        PaidDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled(string reason)
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice");

        Status = InvoiceStatus.Cancelled;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = InvoiceStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddNote(string note)
    {
        Notes = string.IsNullOrEmpty(Notes) ? note : $"{Notes}\n{note}";
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateInvoiceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,
    Failed = 5
}