using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using  Microsoft.AspNetCore.Builder;
using PosSystem.Modules.Users.Models;
using PosSystem.Modules.Users.Services;
using PosSystem.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;


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

            group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateUserRequest req, IUserService svc, CancellationToken ct) =>
            {
                
                var result = await svc.UpdateAsync(id, req, ct);
                return result.IsSuccess? Results.Ok(result.Value) : Results.StatusCode(result.StatusCode);
            }).WithName("UpdateUser")
             .WithSummary("Update user name / phone / email")
             .Produces<UserProfile>(200)
             .Produces(404);
             //Todo: add authorization to allow only admin or the user himself to update his profile
             
        group.MapPut("/{id:guid}/roles",  async ( Guid id, [FromBody] AssignRoleRequest req, IUserService svc, CancellationToken ct) =>
        {
            var result = await svc.AssignRolesAsync(id, req, ct);
            return result.IsSuccess? Results.Ok(result.Value) : Results.StatusCode(result.StatusCode);

        }).WithName("AssignRole")
        .WithSummary("Assign role - Cashier, BranchManager, Account etc")
        .RequireAuthorization("AdminOnly");


        group.MapPut("/{id:guid}/branch", async (Guid id, [FromBody] TransferBranchRequest req, IUserService svc, CancellationToken ct) =>
        {
            var result = await svc.TransferBranchAsync(id, req, ct);
            return result.IsSuccess ? Results.Ok() : Results.StatusCode(result.StatusCode);
        }
        ).WithName("TransferBranch")
         .WithSummary("Transfer user to a different branch")
         .RequireAuthorization("AdminOnly");


         group.MapPatch("/{id:guid}/deactivativate", async (Guid id, IUserService svc, CancellationToken ct) =>
         {
             var result = await svc.DeactivateUserAsync(id, ct);

             return result.IsSuccess ? Results.NoContent() : Results.NotFound();

         }).WithName("Deactive User")
           .WithSummary("Suspends user account (sets IsActive = false)")
           .RequireAuthorization("AdminOnly").Produces(204);

        
         group.MapPatch("/{id:guid}/reactivativate", async (Guid id, IUserService svc, CancellationToken ct) =>
         {
             var result = await svc.ReactivateUserAsync(id, ct);

             return result.IsSuccess ? Results.NoContent() : Results.NotFound();

         }).WithName("Deactive User")
           .WithSummary("Reactivate user account (sets IsActive = true)")
           .RequireAuthorization("AdminOnly").Produces(204);
    }
}