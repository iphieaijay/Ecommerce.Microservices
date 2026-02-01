using InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace InvoiceService.Infrastructure.Persistence;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Invoice Configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.InvoiceNumber)
                .IsUnique();

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.InvoiceDate);

            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CustomerEmail)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PaymentTransactionId)
                .HasMaxLength(100);

            entity.Property(e => e.SubTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            // Owned types for addresses
            entity.OwnsOne(e => e.BillingAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("BillingStreet").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("BillingCity").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("BillingState").HasMaxLength(100);
                address.Property(a => a.ZipCode).HasColumnName("BillingZipCode").HasMaxLength(20);
                address.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(100);
            });

            entity.OwnsOne(e => e.ShippingAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
                address.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20);
                address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
            });

            // Relationship with line items
            entity.HasMany(e => e.LineItems)
                .WithOne()
                .HasForeignKey(li => li.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InvoiceLineItem Configuration
        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.ToTable("InvoiceLineItems");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.ProductId);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ProductSku)
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxRate)
                .HasPrecision(5, 2);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.DiscountPercentage)
                .HasPrecision(5, 2);

            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalPrice)
                .HasPrecision(18, 2);
        });
    }
}
