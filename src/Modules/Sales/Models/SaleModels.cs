namespace PosSystem.Modules.Sales.Models;

// ── Requests ─────────────────────────────────────────────────────────

// What the client sends to create a sale
public record CreateSaleRequest(
    Guid BranchId,
    Guid CashierId,
    Guid? CustomerId,
    string PaymentMethod,             // "Cash", "Card", "MobileMoney", "BankTransfer"
    decimal DiscountAmount,           // discount on the entire sale (e.g., 50.00 off)
    List<CreateSaleItemRequest> Items
);

// Each line item in the sale
public record CreateSaleItemRequest(
    Guid ProductId,
    string ProductName,               // snapshot — in case product name changes later
    string? ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount            // discount per item line (e.g., buy 2 get 10 off)
);

// ── Responses ────────────────────────────────────────────────────────

// What the API returns after creating or querying a sale
public record SaleDto(
    Guid Id,
    Guid BranchId,
    Guid CashierId,
    Guid? CustomerId,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentMethod,
    string Status,
    DateTime CreatedAt,
    List<SaleItemDto> Items
);

// Each line item in the response
public record SaleItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal
);
