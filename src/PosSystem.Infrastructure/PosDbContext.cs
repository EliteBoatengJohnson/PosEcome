
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
