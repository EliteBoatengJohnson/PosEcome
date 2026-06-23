using PosSystem.SharedKernel;

namespace PosSystem.Modules.Sales.Entities;

public class Sale : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid CashierId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;  // Cash, Card, MobileMoney, BankTransfer
    public string Status { get; set; } = "Completed";       // Completed, Cancelled, Refunded, OnHold

    // Navigation: one Sale has many SaleItems
    public ICollection<SaleItem> Items { get; set; } = [];
}

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;   // snapshot at time of sale
    public string? ProductSku { get; set; }                // snapshot of SKU
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}