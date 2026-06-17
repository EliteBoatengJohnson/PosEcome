using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Products.Entities;

namespace PosSystem.Modules.Products.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // ── Table ────────────────────────────────────────────────────
        builder.ToTable("Products");

        // ── Properties ───────────────────────────────────────────────
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.Unit).HasMaxLength(20);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Variants).HasMaxLength(2000);    // JSON string

        // Prices: precision 18, scale 2 → up to 9999999999999999.99
        builder.Property(p => p.SellingPrice).HasPrecision(18, 2);
        builder.Property(p => p.CostPrice).HasPrecision(18, 2);
        builder.Property(p => p.DiscountPercent).HasPrecision(5, 2);  // up to 100.00
        builder.Property(p => p.TaxPercent).HasPrecision(5, 2);

        // ── Indexes ──────────────────────────────────────────────────
        // SKU must be unique — prevents duplicate products
        builder.HasIndex(p => p.Sku).IsUnique();

        // Barcode index (not unique — some products share barcodes)
        builder.HasIndex(p => p.Barcode);

        // ── Relationships ────────────────────────────────────────────
        // Many Products → One Category (FK: CategoryId)
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);  // don't cascade-delete products when category is deleted

        // ── Query Filter ─────────────────────────────────────────────
        // Automatically exclude soft-deleted products from all queries
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
