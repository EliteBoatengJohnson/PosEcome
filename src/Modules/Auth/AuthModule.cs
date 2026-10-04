using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PosSystem.Modules.Auth.Endpoints;
using PosSystem.Modules.Auth.Services;
using PosSystem.SharedKernel;
using PosSystem.SharedKernel.Interfaces;
namespace PosSystem.Modules.Auth;

public class AuthModule : IModuleRegistration
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthProvisioner, AuthProvisionerService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
        => AuthEndpoints.Map(app);
}
