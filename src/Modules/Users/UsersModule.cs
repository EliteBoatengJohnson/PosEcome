using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using PosSystem.Modules.Users.Services;
using PosSystem.Modules.Users.Models;
using PosSystem.SharedKernel;
using PosSystem.Modules.Users.Endpoints;


namespace PosSystem.Modules.Users;
// Admin user management module
// Operator on AppUser entity(same as AuthModule) but with different 
// intent: managging other users rather than currently loggged-in user

public class UsersModule: IModuleRegistration
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    => services.AddScoped<IUserService, UserService>();

    public void MapEndpoints(IEndpointRouteBuilder app)
    => UserEnpoints.Map(app);
}

