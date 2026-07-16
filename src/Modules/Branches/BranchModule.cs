using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Branches;

public class BranchModule : IModuleRegistration
{ 
  
  public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        
    }
}
