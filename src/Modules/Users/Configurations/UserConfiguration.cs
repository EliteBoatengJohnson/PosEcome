using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PosSystem.Modules.Users.Entities;
using System.Text.Json;

namespace PosSystem.Modules.Users.Configurations;

public class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property( u => u.Roles).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),

            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!
            
            ).HasColumnType("nvarchar(1000)")

            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),   // are they equal?
                c => c.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),  // hash
                c => c.ToList()  // create a snapshot (copy) for change tracking
            ));      
            
            builder.HasQueryFilter(u => !u.IsDeleted);


    }
}