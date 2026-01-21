  using Microsoft.EntityFrameworkCore;
    using ProductService.Domain.Entities;

   namespace ProductService.Infrastructure.Persistence
   {
   
        public class ProductDbContext : DbContext
        {
            public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

            public DbSet<Product> Products => Set<Product>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Product>(entity =>
                {
                    entity.ToTable("Products");

                    entity.HasKey(p => p.Id);

                    entity.Property(p => p.Id)
                        .ValueGeneratedNever();

                    entity.Property(p => p.Name)
                        .IsRequired()
                        .HasMaxLength(200);

                    entity.Property(p => p.Description)
                        .HasMaxLength(2000);

                    entity.Property(p => p.SKU)
                        .IsRequired()
                        .HasMaxLength(50);

                    entity.HasIndex(p => p.SKU)
                        .IsUnique();

                    entity.Property(p => p.Price)
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                    entity.Property(p => p.StockQuantity)
                        .IsRequired();

                    entity.Property(p => p.Category)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.HasIndex(p => p.Category);

                    entity.Property(p => p.IsActive)
                        .IsRequired()
                        .HasDefaultValue(true);

                    entity.HasIndex(p => p.IsActive);

                    entity.Property(p => p.CreatedAt)
                        .IsRequired();

                    entity.Property(p => p.UpdatedAt);

                    entity.Property(p => p.CreatedBy)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(p => p.UpdatedBy)
                        .HasMaxLength(100);

                    entity.Property(p => p.RowVersion)
                        .IsRowVersion();
                });
            }

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
        }
   }
