using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosSystem.Modules.Auth.Entities;

namespace PosSystem.Modules.Auth.Configuration;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // ── Table ────────────────────────────────────────────────────
        builder.ToTable("Users");

        // ── Properties ───────────────────────────────────────────────
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.RefreshToken).HasMaxLength(512);

        // ── Roles — List<string> stored as JSON ─────────────────────
        // SQL Server doesn't have an array type, so we serialize
        // ["SuperAdmin", "Cashier"] into a JSON string column.
        // EF automatically converts between List<string> ↔ JSON on read/write.
        builder.Property(u => u.Roles).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
        ).HasColumnType("nvarchar(1000)");

        // ── Indexes ──────────────────────────────────────────────────
        // Email must be unique — prevents duplicate accounts
        builder.HasIndex(u => u.Email).IsUnique();

        // ── Query Filter ─────────────────────────────────────────────
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
