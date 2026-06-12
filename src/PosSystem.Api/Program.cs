// ---------------------------------------------------------------------------
// Program.cs — API Composition Root
// ---------------------------------------------------------------------------
// This is the TOP of the dependency graph. It references:
//   - PosSystem.Infrastructure (for PosDbContext)
//   - All module projects (for endpoints, services, entity configs)
//
// It wires everything together via DI so that:
//   Infrastructure does NOT need to reference any module projects.
//   Modules do NOT need to reference Infrastructure.
//   → No circular dependencies.
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PosSystem.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Module Assembly Registration ─────────────────────────────────────────
// Each module contains IEntityTypeConfiguration<T> classes that tell EF Core
// how to map entities to tables. We register their assemblies here so that
// PosDbContext.OnModelCreating can scan them at runtime.
//
// WHY THIS WORKS:
//   The API project already references every module (for endpoints/services).
//   PosDbContext lives in Infrastructure, which does NOT reference modules.
//   By injecting assemblies via DI, we give PosDbContext the ability to
//   discover module entities without a compile-time circular reference.
var moduleAssemblies = new Assembly[]
{
    typeof(PosSystem.Modules.Products.ProductsModule).Assembly,
    // ↓ Add other module assemblies here as they gain entities ↓
    // typeof(PosSystem.Modules.Sales.SalesModule).Assembly,
    // typeof(PosSystem.Modules.Inventory.InventoryModule).Assembly,
    // typeof(PosSystem.Modules.Auth.AuthModule).Assembly,
    // typeof(PosSystem.Modules.Customers.CustomersModule).Assembly,
};

// Register as singleton — assemblies don't change at runtime
builder.Services.AddSingleton<IEnumerable<Assembly>>(moduleAssemblies);

// ── EF Core + SQL Server ─────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Connection")!;
builder.Services.AddDbContext<PosDbContext>(opts =>
    opts.UseSqlServer(connStr, sqlOpt =>
        {
            sqlOpt.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOpt.CommandTimeout(30);
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

app.Run();
