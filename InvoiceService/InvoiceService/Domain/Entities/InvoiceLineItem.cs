namespace InvoiceService.Domain.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductSku { get; private set; }
    public string? Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }

    private InvoiceLineItem() { }

    public static InvoiceLineItem Create(
        Guid productId,
        string productName,
        string? productSku,
        string? description,
        int quantity,
        decimal unitPrice,
        decimal taxRate = 0,
        decimal discountPercentage = 0)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        var lineItem = new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            ProductSku = productSku,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            DiscountPercentage = discountPercentage
        };

        lineItem.CalculateAmounts();
        return lineItem;
    }

    private void CalculateAmounts()
    {
        var subtotal = Quantity * UnitPrice;
        DiscountAmount = subtotal * (DiscountPercentage / 100);
        var amountAfterDiscount = subtotal - DiscountAmount;
        TaxAmount = amountAfterDiscount * (TaxRate / 100);
        TotalPrice = amountAfterDiscount + TaxAmount;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        Quantity = quantity;
        CalculateAmounts();
    }
}