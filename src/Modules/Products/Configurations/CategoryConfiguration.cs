// ---------------------------------------------------------------------------
// CategoryConfiguration.cs — EF Core entity configuration for Category
// ---------------------------------------------------------------------------
// Same pattern as ProductConfiguration. Lives in the Products module,
// discovered at runtime by PosDbContext.OnModelCreating via assembly scanning.
// ---------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Products.Entities;

namespace PosSystem.Modules.Products.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Table name
        builder.ToTable("Categories");

        // Primary key (inherited from BaseEntity)
        builder.HasKey(c => c.Id);

        // Required fields
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Optional fields
        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // Self-referencing parent for category hierarchy
        builder.Property(c => c.ParentId);

        // Soft-delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
        
        // parent category self referencing 
        builder.HasOne<Category>()             // A category can have a Parent Category
            .WithMany()                        // We don't have a SubCategories list property, so leave this empty
            .HasForeignKey(c => c.ParentId)    // It links using the ParentId column
            .OnDelete(DeleteBehavior.NoAction); // If a parent is deleted, don't break the database
    }
}
