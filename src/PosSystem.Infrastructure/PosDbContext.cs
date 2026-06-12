// ---------------------------------------------------------------------------
// PosDbContext.cs — The central EF Core DbContext for the PosSystem
// ---------------------------------------------------------------------------
// DESIGN DECISION:
//   Infrastructure does NOT reference any module projects.
//   Instead, module assemblies are injected at runtime via IEnumerable<Assembly>.
//   Each module registers its entities using IEntityTypeConfiguration<T> classes,
//   which are discovered by ApplyConfigurationsFromAssembly() in OnModelCreating.
//
// WHY:
//   This breaks the circular dependency:
//     ❌ BEFORE: Infrastructure → Products → Infrastructure (circular!)
//     ✅ AFTER:  Infrastructure → SharedKernel (no modules)
//               API (composition root) → Infrastructure + all modules
//
// HOW IT WORKS:
//   1. Each module defines IEntityTypeConfiguration<T> for its entities
//   2. The API project registers module assemblies as IEnumerable<Assembly>
//   3. PosDbContext scans those assemblies in OnModelCreating
//   4. EF discovers entities + their configurations without compile-time refs
// ---------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace PosSystem.Infrastructure;

public class PosDbContext : DbContext
{
    // Module assemblies are injected by the DI container.
    // The API project (composition root) registers them at startup.
    private readonly IEnumerable<Assembly> _moduleAssemblies;

    // FIX: Constructor parameter was DbContext<PosDbContext> (which doesn't exist).
    //      Correct type is DbContextOptions<PosDbContext>.
    public PosDbContext(
        DbContextOptions<PosDbContext> options,
        IEnumerable<Assembly> moduleAssemblies)
        : base(options)
    {
        _moduleAssemblies = moduleAssemblies;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scan each module assembly for IEntityTypeConfiguration<T> implementations.
        // This is how entities are registered WITHOUT Infrastructure referencing modules.
        foreach (var assembly in _moduleAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
