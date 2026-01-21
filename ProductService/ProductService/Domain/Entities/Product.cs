namespace ProductService.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string SKU { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int StockQuantity { get; private set; }
        public string Category { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public string CreatedBy { get; private set; } = string.Empty;
        public string? UpdatedBy { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        private Product() { } 

        public static Product Create(
            string name,
            string description,
            string sku,
            decimal price,
            int stockQuantity,
            string category,
            string createdBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be empty", nameof(sku));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description ?? string.Empty,
                SKU = sku,
                Price = price,
                StockQuantity = stockQuantity,
                Category = category ?? "General",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }

        public void Update(
            string name,
            string description,
            decimal price,
            int stockQuantity,
            string category,
            string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            StockQuantity = stockQuantity;
            Category = category ?? "General";
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void UpdateStock(int quantity)
        {
            if (StockQuantity + quantity < 0)
                throw new InvalidOperationException("Insufficient stock");

            StockQuantity += quantity;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate(string updatedBy)
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void Activate(string updatedBy)
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
