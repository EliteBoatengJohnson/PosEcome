using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Products.Entities;

namespace PosSystem.Modules.Products.Configuration;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ── Table ────────────────────────────────────────────────────
        builder.ToTable("Categories");

        // ── Properties ───────────────────────────────────────────────
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        // ── Indexes ──────────────────────────────────────────────────
        builder.HasIndex(c => c.Name);

        // ── Self-referencing relationship ────────────────────────────
        // A category can have a parent category (ParentId is nullable).
        // e.g., "Electronics" → "Phones" → "Smartphones"
        // If parent is deleted, set children's ParentId to NULL.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Query Filter ─────────────────────────────────────────────
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
