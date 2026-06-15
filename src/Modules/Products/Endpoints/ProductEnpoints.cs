using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PosSystem.Modules.Products.Models;
using PosSystem.Modules.Products.Services;

namespace PosSystem.Modules.Products.Endpoints;

public static class ProductEnpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", async (
            IProductService svc,
            int page = 1, int pageSize = 20,
            Guid? categoryId = null, string? search = null,
            CancellationToken ct = default) =>
        {
            var result = await svc.GetAllAsync(page, pageSize, categoryId, search, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.StatusCode(result.StatusCode);
        }).WithName("GetProducts");

        group.MapGet("/{id:guid}", async (Guid id, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("GetProductById");

        group.MapGet("/barcode/{barcode}", async (string barcode, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByBarcodeAsync(barcode, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("GetProductByBarcode");

        group.MapPost("/", async (CreateProductRequest req, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/products/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        }).WithName("CreateProduct");

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest req, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.UpdateAsync(id, req, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("UpdateProduct");

        group.MapDelete("/{id:guid}", async (Guid id, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.DeleteAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).WithName("DeleteProduct");

        // ── Categories ──
        var cats = app.MapGroup("/api/v1/categories").WithTags("Categories");

        cats.MapGet("/", async (IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.GetCategoriesAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.StatusCode(result.StatusCode);
        }).WithName("GetCategories");

        cats.MapPost("/", async (CreateCategoryRequest req, IProductService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateCategoryAsync(req, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/categories/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        }).WithName("CreateCategory");
    }
}