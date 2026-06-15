using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;
using PosSystem.Modules.Products.Entities;
using PosSystem.Modules.Products.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Products.Services;

// ProductsService injects PosDbContext (from Infrastructure).
// This is safe because: Products → Infrastructure → SharedKernel (one-way, no circle).
// Infrastructure does NOT reference Products back.
//
// Since PosDbContext has no DbSet<Product> property (it doesn't know about Product),
// we use db.Set<Product>() which is the generic EF Core way to access any entity
// that was registered via IEntityTypeConfiguration in OnModelCreating.
public class ProductsService(PosDbContext db) : IProductService
{
    // Convenience accessors — db.Set<T>() returns DbSet<T> for any configured entity
    private DbSet<Product> Products => db.Set<Product>();
    private DbSet<Category> Categories => db.Set<Category>();

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(
        int page, int pageSize, Guid? categoryId, string? search, CancellationToken ct = default)
    {
        var q = Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();

        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search)
                          || p.Sku.Contains(search)
                          || (p.Barcode != null && p.Barcode.Contains(search)));

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Result<PagedResult<ProductDto>>.Ok(new(items.Select(ToDto), page, pageSize, total));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct = default)
    {
        if (await Products.AnyAsync(p => p.Sku == req.Sku, ct))
            return Result<ProductDto>.Fail("A product with this SKU already exists");

        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Sku = req.Sku,
            Barcode = req.Barcode,
            CategoryId = req.CategoryId,
            SellingPrice = req.SellingPrice,
            CostPrice = req.CostPrice,
            DiscountPercent = req.DiscountPercent,
            TaxPercent = req.TaxPercent,
            Unit = req.Unit,
            TrackExpiry = req.TrackExpiry,
            Variants = req.Variants
        };

        Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Result<ProductDto>.Created(ToDto(product));
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);
        return p is null ? Result<ProductDto>.NotFound("Product not found") : Result<ProductDto>.Ok(ToDto(p));
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest req, CancellationToken ct = default)
    {
        var p = await Products.FindAsync([id], ct);
        if (p is null) return Result<ProductDto>.NotFound("Product not found");

        if (req.Name is not null) p.Name = req.Name;
        if (req.SellingPrice.HasValue) p.SellingPrice = req.SellingPrice.Value;
        if (req.CostPrice.HasValue) p.CostPrice = req.CostPrice.Value;
        if (req.IsActive.HasValue) p.IsActive = req.IsActive.Value;
        if (req.CategoryId.HasValue) p.CategoryId = req.CategoryId.Value;

        await db.SaveChangesAsync(ct);
        return Result<ProductDto>.Ok(ToDto(p));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await Products.FindAsync([id], ct);
        if (p is null) return Result<bool>.NotFound("Product not found");

        p.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var p = await Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode || p.Sku == barcode, ct);
        return p is null ? Result<ProductDto>.NotFound("Product not found") : Result<ProductDto>.Ok(ToDto(p));
    }

    public Task<Result<int>> BulkImportAsync(Stream csvStream, CancellationToken ct = default)
    {
        // TODO: parse CSV with CsvHelper, validate, insert
        return Task.FromResult(Result<int>.Ok(0));
    }

    public Task<Result<byte[]>> ExportAsync(CancellationToken ct = default)
    {
        // TODO: use ClosedXML to generate Excel
        return Task.FromResult(Result<byte[]>.Ok(Array.Empty<byte>()));
    }

    public async Task<Result<List<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await Categories.ToListAsync(ct);
        return Result<List<CategoryDto>>.Ok(
            cats.Select(c => new CategoryDto(c.Id, c.Name, c.ParentId, c.Description)).ToList());
    }

    public async Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct = default)
    {
        var cat = new Category { Name = req.Name, ParentId = req.ParentId, Description = req.Description };
        Categories.Add(cat);
        await db.SaveChangesAsync(ct);
        return Result<CategoryDto>.Created(new(cat.Id, cat.Name, cat.ParentId, cat.Description));
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Sku, p.Barcode,
        p.CategoryId, p.Category?.Name, p.SellingPrice, p.CostPrice,
        p.DiscountPercent, p.TaxPercent, p.Unit, p.IsActive);
}