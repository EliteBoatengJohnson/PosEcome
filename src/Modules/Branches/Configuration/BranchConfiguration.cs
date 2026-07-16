using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Branches.Entities;

namespace  PosSystem.Modules.Branches.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.code).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Email).HasMaxLength(256);
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}