using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PosSystem.Modules.Auth.Models;
using PosSystem.Modules.Auth.Services;

namespace PosSystem.Modules.Auth.Endpoints;

public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, IAuthService svc, CancellationToken ct) =>
        {
            var result = await svc.RegisterAsync(req, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/auth/me", result.Value)
                : Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
        }).WithName("Register").AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, IAuthService svc, CancellationToken ct) =>
        {
            var result = await svc.LoginAsync(req, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Json(
                new { error = result.Error }, statusCode: result.StatusCode);
        }).WithName("Login").AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokens req, IAuthService svc, CancellationToken ct) =>
        {
            var result = await svc.RefreshTokenAsync(req.RefreshToken, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Json(
                new { error = result.Error }, statusCode: result.StatusCode);
        }).WithName("RefreshToken").AllowAnonymous();

        group.MapPost("/logout", async (RefreshTokens req, IAuthService svc, CancellationToken ct) =>
        {
            var result = await svc.LogoutAsync(req.RefreshToken, ct);
            return Results.Ok(new { message = "Logged out" });
        }).WithName("Logout");

        group.MapGet("/me", async (HttpContext http, IAuthService svc, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? http.User.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var result = await svc.GetCurrentUserAsync(userId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("GetCurrentUser").RequireAuthorization();
    }
}