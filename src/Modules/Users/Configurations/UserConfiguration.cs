using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Users.Entities;

namespace PosSystem.Modules.Users.Configurations;

public class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure the User entity here. Example:
        // builder.HasKey(u => u.Id);
        // builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
    }
}