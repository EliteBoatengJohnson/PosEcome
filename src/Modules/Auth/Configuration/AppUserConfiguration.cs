using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Auth.Entities;

namespace PosSystem.Modules.Auth.Configuration;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // ── Table ────────────────────────────────────────────────────
        builder.ToTable("Users", "auth");

        // ── Properties ───────────────────────────────────────────────
       
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(512);

        
        // ── Indexes ──────────────────────────────────────────────────
        // Email must be unique — prevents duplicate accounts
        builder.HasIndex(u => u.Email).IsUnique();

        // ── Query Filter ─────────────────────────────────────────────
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
