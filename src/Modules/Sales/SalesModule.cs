using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PosSystem.Modules.Sales.Endpoints;
using PosSystem.Modules.Sales.Services;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Sales;

public class SalesModule : IModuleRegistration
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<ISalesService, SalesService>();

    public void MapEndpoints(IEndpointRouteBuilder app)
        => SalesEndpoints.Map(app);
}
