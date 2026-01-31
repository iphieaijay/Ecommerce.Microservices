
using OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace OrderService.Infrastructure.Persistence
    {
        public class OrderDbContext : DbContext
        {
            public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

           public DbSet<Order> Orders { get; set; }
            public DbSet<OrderItem> OrderItems { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Order>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                    entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                    entity.HasIndex(e => e.OrderNumber).IsUnique();

                    entity.HasMany(e => e.OrderItems)
                          .WithOne(e => e.Order)
                          .HasForeignKey(e => e.OrderId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                modelBuilder.Entity<OrderItem>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                    entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                    entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                });
            }
        }
    }

