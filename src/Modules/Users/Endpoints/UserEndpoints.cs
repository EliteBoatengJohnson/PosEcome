using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using  Microsoft.AspNetCore.Builder;
using PosSystem.Modules.Users.Models;
using PosSystem.Modules.Users.Services;
using PosSystem.SharedKernel;
using Microsoft.AspNetCore.Mvc;


namespace PosSystem.Modules.Users.Endpoints;

public static class UserEnpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users");
            //.RequireAuthorization();


            group.MapGet("/",  async (int page,int pageSize, Guid? branchId, string? role, string? search, IUserService svc, CancellationToken ct ) =>
            {
                var result = await svc.GetAllAsync(page, pageSize, branchId, role, search, ct);
                return result.IsSuccess? Results.Ok(result.Value): Results.StatusCode(result.StatusCode);
            }
            ).WithName("GetUsers");

            group.MapGet("/{id:guid}", async (Guid id, IUserService svc, CancellationToken ct) =>
            {
                var result = await svc.GetByIdAsync(id,ct);
                return result.IsSuccess? Results.Ok(result.Value) : Results.NotFound(result.Error);
            }).WithName("GetUserById")
              .WithSummary("List all users - filter by branch, role or search term")
              .Produces<PagedResult<UserProfile>>();


           group.MapPost("/", async ([FromBody] CreateUserRequest req, IUserService svc, CancellationToken ct)=>
           {
               var result = await svc.CreateAsync(req, ct);
               return result.IsSuccess? Results.Created($"/api/v1/users/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error);
           }).WithName("CreateUser")
              .WithSummary("Create new staff account")
              .Produces<UserProfile>(201);
              //todo: add validation for request body
    }
}