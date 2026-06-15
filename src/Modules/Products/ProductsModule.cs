using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PosSystem.Modules.Products.Services;
using PosSystem.Modules.Products.Endpoints;
using PosSystem.SharedKernel;


namespace PosSystem.Modules.Products;

public class ProductsModule : IModuleRegistration
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)

    => services.AddScoped<IProductService, ProductsService>();

    public void MapEndpoints(IEndpointRouteBuilder app)
        => ProductEndpoints.Map(app);
}  