using PosSystem.SharedKernel;

namespace PosSystem.Modules.Products.Entities;

public class Product : BaseEntity
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
    public string? ImageUrl { get; set; }   
    public bool TrackExpiry  { get; set; }
    public string? Unit {get; set;}
    public string? Variants {get; set;} // JSON: [{"size": "L", "color": "Red"}]
    public bool IsActive { get; set; } = true;
}

public class Category : BaseEntity
{
    public string Name { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public string? Description { get; set; }
}