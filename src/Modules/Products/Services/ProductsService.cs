using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
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
        var q = Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();// loads active products as queryable

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

    public async Task<Result<int>> BulkImportAsync(Stream csvStream, CancellationToken ct = default)
    {
        // ── 1. Configure CsvHelper reader ──────────────────────────────
        // CsvHelper reads a Stream via StreamReader → CsvReader → GetRecords<T>.
        // The ClassMap (ProductCsvMap) controls column-to-property mapping.
        using var reader = new StreamReader(csvStream);
        // creates a base configuration for the type csv header to be imported
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,          // first row is column names
            HeaderValidated = null,          // don't throw if extra columns exist
            MissingFieldFound = null,        // don't throw on missing optional columns
            TrimOptions = TrimOptions.Trim,  // strip whitespace from values
        });

        // Register the ClassMap so CsvHelper knows how to map CSV columns → ProductCsvRow
        csv.Context.RegisterClassMap<ProductCsvMap>();

        // ── 2. Read all rows from CSV ──────────────────────────────────
        List<ProductCsvRow> rows;
        try
        {
            rows = csv.GetRecords<ProductCsvRow>().ToList();
        }
        catch (CsvHelperException ex)
        {
            return Result<int>.Fail($"CSV parsing error: {ex.Message}");
        }

        if (rows.Count == 0)
            return Result<int>.Fail("CSV file is empty or has no data rows");

        // ── 3. Validate & deduplicate against existing SKUs ────────────
        var incomingSkus = rows.Select(r => r.Sku).Distinct().ToList();
        var existingSkus = await Products
            .Where(p => incomingSkus.Contains(p.Sku))
            .Select(p => p.Sku)
            .ToListAsync(ct);

        var existingSkuSet = existingSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var productsToInsert = new List<Product>();

        foreach (var row in rows)
        {
            // Skip rows with duplicate SKUs (already in DB or repeated in CSV)
            if (existingSkuSet.Contains(row.Sku) || !seenSkus.Add(row.Sku))
                continue;

            productsToInsert.Add(new Product
            {
                Name = row.Name,
                Description = row.Description,
                Sku = row.Sku,
                Barcode = row.Barcode,
                CategoryId = row.CategoryId,
                SellingPrice = row.SellingPrice,
                CostPrice = row.CostPrice,
                DiscountPercent = row.DiscountPercent,
                TaxPercent = row.TaxPercent,
                Unit = row.Unit,
                TrackExpiry = row.TrackExpiry,
                Variants = row.Variants,
            });
        }

        if (productsToInsert.Count == 0)
            return Result<int>.Ok(0);

        // ── 4. Batch insert ────────────────────────────────────────────
        // AddRange + SaveChanges sends a single INSERT with all rows.
        Products.AddRange(productsToInsert);
        await db.SaveChangesAsync(ct);

        return Result<int>.Ok(productsToInsert.Count);
    }

    public async Task<Result<byte[]>> ExportAsync(CancellationToken ct = default)
    {
        // ── 1. Fetch all active products with their category names ──
        var products = await Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        // ── 2. Create workbook and worksheet ────────────────────────
        using var workbook = new XLWorkbook();
        //giving your sheets name
        var ws = workbook.Worksheets.Add("Products");

        // ── 3. Write header row ─────────────────────────────────────
        var headers = new[]
        {
            "Name", "Description", "SKU", "Barcode", "Category",
            "Selling Price", "Cost Price", "Discount %", "Tax %",
            "Unit", "Track Expiry", "Active"
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        // Style the header row
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // ── 4. Write data rows ──────────────────────────────────────
        for (var row = 0; row < products.Count; row++)
        {
            var p = products[row];
            var r = row + 2; // row 1 is header, data starts at row 2

            ws.Cell(r, 1).Value = p.Name;
            ws.Cell(r, 2).Value = p.Description ?? "";
            ws.Cell(r, 3).Value = p.Sku;
            ws.Cell(r, 4).Value = p.Barcode ?? "";
            ws.Cell(r, 5).Value = p.Category?.Name ?? "";
            ws.Cell(r, 6).Value = p.SellingPrice;
            ws.Cell(r, 7).Value = p.CostPrice;
            ws.Cell(r, 8).Value = p.DiscountPercent ?? 0;
            ws.Cell(r, 9).Value = p.TaxPercent ?? 0;
            ws.Cell(r, 10).Value = p.Unit ?? "";
            ws.Cell(r, 11).Value = p.TrackExpiry ? "Yes" : "No";
            ws.Cell(r, 12).Value = p.IsActive ? "Yes" : "No";
        }

        // ── 5. Format currency and percentage columns ────────────────
        var lastRow = products.Count + 1;
        ws.Range(2, 6, lastRow, 7).Style.NumberFormat.Format = "#,##0.00";  // prices
        ws.Range(2, 8, lastRow, 9).Style.NumberFormat.Format = "0.00";      // percentages

        // ── 6. Auto-fit column widths to content ────────────────────
        ws.Columns().AdjustToContents();

        // ── 7. Add table-style borders ──────────────────────────────
        var dataRange = ws.Range(1, 1, lastRow, headers.Length);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorderColor = XLColor.DarkGray;

        // ── 8. Save to byte array ───────────────────────────────────
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return Result<byte[]>.Ok(stream.ToArray());
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