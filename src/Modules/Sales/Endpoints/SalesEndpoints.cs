using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PosSystem.Modules.Sales.Models;
using PosSystem.Modules.Sales.Services;

namespace PosSystem.Modules.Sales.Endpoints;

public static class SalesEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sales").WithTags("Sales");

        // Create a new sale
        group.MapPost("/", async (CreateSaleRequest req, ISalesService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateSaleAsync(req, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/sales/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        }).WithName("CreateSale");

        // Get a sale by ID
        group.MapGet("/{id:guid}", async (Guid id, ISalesService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("GetSaleById");

        // List all sales (paged, filterable by branch and status)
        group.MapGet("/", async (
            ISalesService svc,
            int page = 1, int pageSize = 20,
            Guid? branchId = null, string? status = null,
            CancellationToken ct = default) =>
        {
            var result = await svc.GetAllAsync(page, pageSize, branchId, status, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.StatusCode(result.StatusCode);
        }).WithName("GetSales");

        // Cancel a sale
        group.MapPut("/{id:guid}/cancel", async (Guid id, ISalesService svc, CancellationToken ct) =>
        {
            var result = await svc.CancelSaleAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Json(
                new { error = result.Error }, statusCode: result.StatusCode);
        }).WithName("CancelSale");
    }
}
