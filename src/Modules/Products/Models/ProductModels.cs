
namespace PosSystem.Modules.Products.Models;

// DTO for creating product request
public record CreateProductRequest(
    string Name,
    string? Description,
    string Sku,
    string? Barcode,
    Guid CategoryId,
    decimal SellingPrice,
    decimal CostPrice,
    decimal? DiscountPercent,
    decimal? TaxPercent,
    string? Unit,
    bool TrackExpiry,
    string? Variants
    );
public record UpdateProductRequest(
    string? Name,
    string? Description, 
    string? Barcode,
    Guid? CategoryId,
    decimal? SellingPrice, 
    decimal? CostPrice,
    decimal? DiscountPercent, 
    decimal? TaxPercent, 
    bool? IsActive);
 
public record ProductDto(
    Guid Id, 
    string Name,
    string? Description, 
    string SKU,
    string? Barcode,
    Guid CategoryId,
    string? CategoryName,
    decimal SellingPrice,
    decimal CostPrice,
    decimal? DiscountPercent, 
    decimal? TaxPercent,
    string? Unit, 
    bool IsActive);
 
public record CreateCategoryRequest(string Name, Guid? ParentId, string? Description);
 
public record CategoryDto(Guid Id, string Name, Guid? ParentId, string? Description);