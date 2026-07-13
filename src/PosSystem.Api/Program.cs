// ---------------------------------------------------------------------------
// Program.cs — API Composition Root
// ---------------------------------------------------------------------------
// This is the TOP of the dependency graph. It references:
//   - PosSystem.Infrastructure (for PosDbContext)
//   - All module projects (for endpoints, services, entity configs)
//
// It wires everything together via DI so that:
//   Infrastructure does NOT need to reference any module projects.
//   → No circular dependencies.
//
// STARTUP FLOW:
//   1. Configure logging (Serilog)
//   2. Register module assemblies (for EF entity discovery)
//   3. Configure JWT authentication (read secret key, set validation rules)
//   4. Configure EF Core + SQL Server (connection string, retry policy)
//   5. Register module services (each module wires its own DI)
//   6. Build the app
//   7. Set up middleware pipeline (auth → authorization → endpoints)
//   8. Run
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PosSystem.Infrastructure;
using PosSystem.Modules.Auth;
using PosSystem.Modules.Products;
using PosSystem.Modules.Sales;
using PosSystem.SharedKernel;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────
// Replaces the default ASP.NET Core logger with Serilog.
// Configuration (log levels, sinks) is read from appsettings.json "Serilog" section.
// This gives us structured logging with console + file sinks.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Module Assembly Registration ─────────────────────────────────────────
// PROBLEM:  PosDbContext lives in Infrastructure, but entity classes
//           (Product, AppUser, etc.) live in module projects.
//           Infrastructure does NOT reference modules (to avoid circular deps).
//
// SOLUTION: We pass module assemblies into the DI container here.
//           PosDbContext receives them via constructor injection and calls
//           modelBuilder.ApplyConfigurationsFromAssembly(assembly) to discover
//           all IEntityTypeConfiguration<T> classes at runtime.
//
// WHEN TO UPDATE: Add a new entry here whenever a module defines DB entities.
var moduleAssemblies = new Assembly[]
{
    typeof(PosSystem.Modules.Products.ProductsModule).Assembly,  // Product, Category entities
    typeof(PosSystem.Modules.Auth.AuthModule).Assembly,           // AppUser entity
    typeof(PosSystem.Modules.Sales.SalesModule).Assembly,      // sales entities
    typeof(PosSystem.Modules.Users.UsersModule).Assembly,      // users entiti
};

// Singleton because the list of assemblies never changes after startup.
builder.Services.AddSingleton<IEnumerable<Assembly>>(moduleAssemblies);

// ── JWT Authentication ───────────────────────────────────────────────────
// Reads the "Jwt" section from appsettings.json:
//   Issuer:          who issued the token (must match token's "iss" claim)
//   Audience:        who the token is for (must match token's "aud" claim)
//   SecretKey:       HMAC-SHA256 signing key (same key signs & verifies)
//   ExpiryMinutes:   how long access tokens are valid (used by TokenService)
//   RefreshExpiryDays: how long refresh tokens are valid (used by TokenService)
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);

// Set JWT Bearer as the default auth scheme.
// This means every request automatically checks for "Authorization: Bearer <token>".
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    // These rules define how incoming JWTs are validated on every request.
    // If ANY check fails, the request gets a 401 Unauthorized response.
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,             // token's "iss" must match our Issuer
        ValidateAudience = true,           // token's "aud" must match our Audience
        ValidateLifetime = true,           // token must not be expired
        ValidateIssuerSigningKey = true,   // token's signature must be valid
        ValidIssuer = jwt["Issuer"],       // expected issuer: "pos-system"
        ValidAudience = jwt["Audience"],   // expected audience: "pos-client"
        IssuerSigningKey = new SymmetricSecurityKey(key),  // the signing key to verify against
        ClockSkew = TimeSpan.Zero          // tokens expire exactly on time (default allows 5 min grace)
    };
});

// Enables [Authorize] attributes and .RequireAuthorization() on endpoints.
// Without this, authorization policies won't be enforced even if authentication works.
builder.Services.AddAuthorization();

// ── EF Core + SQL Server ─────────────────────────────────────────────────
// Registers PosDbContext as a scoped service (one instance per HTTP request).
// The connection string "Connection" comes from appsettings.json → ConnectionStrings.
var connStr = builder.Configuration.GetConnectionString("Connection")!;
builder.Services.AddDbContext<PosDbContext>(opts =>
    opts.UseSqlServer(connStr, sqlOpt =>
        {
            // Automatically retry failed DB operations (network blips, transient errors).
            // Retries up to 3 times with a max 5-second delay between attempts.
            sqlOpt.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);

            // If a single query takes longer than 30 seconds, it gets cancelled.
            sqlOpt.CommandTimeout(30);
        }));

// ── Module Service Registration ──────────────────────────────────────────
// Each module implements IModuleRegistration (defined in SharedKernel).
// RegisterServices() lets each module register its own services (e.g.,
// ProductsModule registers IProductService → ProductsService as Scoped,
// AuthModule registers TokenService as Singleton and IAuthService as Scoped).
//
// This keeps Program.cs clean — you don't list every service here.
// Just add a new module instance and it handles its own DI.
var modules = new IModuleRegistration[]
{
    new ProductsModule(),
    new AuthModule(),
    new SalesModule(),
    // ↓ Add other modules here as you implement them ↓
};

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

// ── OpenAPI / Scalar ─────────────────────────────────────────────────────
// AddOpenApi() generates the OpenAPI spec (JSON) for all endpoints.
// Scalar provides a beautiful API docs UI (alternative to Swagger UI).
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────
// ORDER MATTERS! Middleware runs top-to-bottom on requests, bottom-to-top on responses.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                    // serves /openapi/v1.json
    app.MapScalarApiReference();         // serves interactive docs at /scalar/v1
}

// Redirects HTTP → HTTPS for security.
app.UseHttpsRedirection();

// UseAuthentication: reads the JWT from the "Authorization: Bearer <token>" header,
// validates it using the TokenValidationParameters above, and populates HttpContext.User
// with the claims (userId, email, roles, etc.).
// MUST come before UseAuthorization.
app.UseAuthentication();

// UseAuthorization: checks if the authenticated user meets the requirements
// of [Authorize] attributes or .RequireAuthorization() on endpoints.
// Endpoints marked with .AllowAnonymous() skip this check.
app.UseAuthorization();

// ── Module Endpoint Registration ─────────────────────────────────────────
// Each module maps its own API routes (e.g., /api/v1/products, /api/v1/auth).
// This is the same modules array from above — MapEndpoints() is the second
// method of the IModuleRegistration interface.
foreach (var module in modules)
    module.MapEndpoints(app);

app.Run();
