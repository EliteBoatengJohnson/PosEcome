using CsvHelper.Configuration;

namespace PosSystem.Modules.Products.Models;

/// <summary>
/// Represents a single row in the CSV file for bulk product import.
/// Column headers in the CSV must match these property names (case-insensitive)
/// unless overridden in ProductCsvMap below.
///
/// Example CSV:
/// Name,Description,Sku,Barcode,CategoryId,SellingPrice,CostPrice,DiscountPercent,TaxPercent,Unit,TrackExpiry,Variants
/// "Widget A","A great widget","SKU-001","1234567890",3fa85f64-5717-4562-b3fc-2c963f66afa6,19.99,12.50,,,pcs,false,
/// </summary>
public class ProductCsvRow
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public Guid CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? TaxPercent { get; set; }
    public string? Unit { get; set; }
    public bool TrackExpiry { get; set; }
    public string? Variants { get; set; }
}

/// <summary>
/// CsvHelper ClassMap — maps CSV column headers to ProductCsvRow properties.
/// This gives you full control over column names, optional columns, and type conversion.
/// If your CSV headers already match the property names exactly, this map is optional
/// but it's best practice to be explicit.
/// </summary>
public sealed class ProductCsvMap : ClassMap<ProductCsvRow>
{
    public ProductCsvMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Description).Name("Description").Optional();
        Map(m => m.Sku).Name("Sku", "SKU");                       // accept both
        Map(m => m.Barcode).Name("Barcode").Optional();
        Map(m => m.CategoryId).Name("CategoryId");
        Map(m => m.SellingPrice).Name("SellingPrice");
        Map(m => m.CostPrice).Name("CostPrice");
        Map(m => m.DiscountPercent).Name("DiscountPercent").Optional();
        Map(m => m.TaxPercent).Name("TaxPercent").Optional();
        Map(m => m.Unit).Name("Unit").Optional();
        Map(m => m.TrackExpiry).Name("TrackExpiry").Optional();
        Map(m => m.Variants).Name("Variants").Optional();
    }
}
