
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