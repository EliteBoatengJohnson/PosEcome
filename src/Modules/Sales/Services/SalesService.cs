using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;
using PosSystem.Modules.Sales.Entities;
using PosSystem.Modules.Sales.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Sales.Services;

public class SalesService(PosDbContext db) : ISalesService
{
    private DbSet<Sale> Sales => db.Set<Sale>();

    // ── Create Sale ──────────────────────────────────────────────────
    public async Task<Result<SaleDto>> CreateSaleAsync(CreateSaleRequest request, CancellationToken ct = default)
    {
        // 1. Map each line item from the request
        var items = request.Items.Select(i => new SaleItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductSku = i.ProductSku,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountAmount = i.DiscountAmount,
            LineTotal = (i.Quantity * i.UnitPrice) - i.DiscountAmount,
        }).ToList();

        // 2. Calculate sale totals
        var subTotal = items.Sum(i => i.LineTotal);
        var taxAmount = subTotal * 0.15m;   // 15% tax — make configurable later

        var sale = new Sale
        {
            BranchId = request.BranchId,
            CashierId = request.CashierId,
            CustomerId = request.CustomerId,
            PaymentMethod = request.PaymentMethod,
            DiscountAmount = request.DiscountAmount,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TotalAmount = subTotal + taxAmount - request.DiscountAmount,
            Status = "Completed",
            Items = items,
        };

        // 3. Save to database
        Sales.Add(sale);
        await db.SaveChangesAsync(ct);

        // 4. Return the created sale
        return Result<SaleDto>.Created(ToDto(sale));
    }

    // ── Get Sale by ID ───────────────────────────────────────────────
    public async Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return sale is null
            ? Result<SaleDto>.NotFound("Sale not found")
            : Result<SaleDto>.Ok(ToDto(sale));
    }

    // ── Get All Sales (paged) ────────────────────────────────────────
    public async Task<Result<PagedResult<SaleDto>>> GetAllAsync(
        int page, int pageSize, Guid? branchId, string? status, CancellationToken ct = default)
    {
        var q = Sales.Include(s => s.Items).AsQueryable();

        // Optional filters
        if (branchId.HasValue)
            q = q.Where(s => s.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(s => s.Status == status);

        // Order by newest first
        q = q.OrderByDescending(s => s.CreatedAt);

        // Count total before paging
        var total = await q.CountAsync(ct);

        // Apply pagination: skip previous pages, take current page
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<SaleDto>>.Ok(
            new PagedResult<SaleDto>(items.Select(ToDto), page, pageSize, total));
    }

    // ── Cancel Sale ──────────────────────────────────────────────────
    public async Task<Result<SaleDto>> CancelSaleAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sale is null)
            return Result<SaleDto>.NotFound("Sale not found");

        if (sale.Status == "Cancelled")
            return Result<SaleDto>.Fail("Sale is already cancelled", 400);

        sale.Status = "Cancelled";
        await db.SaveChangesAsync(ct);

        return Result<SaleDto>.Ok(ToDto(sale));
    }

    // ── Mapping helper ───────────────────────────────────────────────
    private static SaleDto ToDto(Sale s) => new(
        s.Id,
        s.BranchId,
        s.CashierId,
        s.CustomerId,
        s.SubTotal,
        s.TaxAmount,
        s.DiscountAmount,
        s.TotalAmount,
        s.PaymentMethod,
        s.Status,
        s.CreatedAt,
        s.Items.Select(i => new SaleItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.ProductSku,
            i.Quantity,
            i.UnitPrice,
            i.DiscountAmount,
            i.LineTotal
        )).ToList()
    );
}
