// ---------------------------------------------------------------------------
// ProductConfiguration.cs — EF Core entity configuration for Product
// ---------------------------------------------------------------------------
// This class tells EF Core how to map the Product entity to a database table.
// It lives in the Products MODULE (not Infrastructure), so there's no circular
// dependency. EF discovers it via ApplyConfigurationsFromAssembly() in PosDbContext.
// ---------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Products.Entities;

namespace PosSystem.Modules.Products.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Table name
        builder.ToTable("Products");

        // Primary key (inherited from BaseEntity)
        builder.HasKey(p => p.Id);

        // Required fields
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(100);

        // Optional fields with max lengths
        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Unit)
            .HasMaxLength(50);

        // Precision for money columns
        builder.Property(p => p.SellingPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(p => p.TaxPercent)
            .HasPrecision(5, 2);

        // Variants is stored as JSON text
        builder.Property(p => p.Variants)
            .HasMaxLength(2000);

        // Unique index on SKU
        builder.HasIndex(p => p.Sku)
            .IsUnique();

        // Soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
        
        // Navigation Properties
        builder.HasOne(p => p.Category) // Product has one category
            .WithMany(c => c.Products) // A Category has may products
            .HasForeignKey(p => p.CategoryId) // The link is connected via CategoryId
            .OnDelete(DeleteBehavior.Restrict); // Blocks category deletion if it has product
    }
}
