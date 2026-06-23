using PosSystem.Modules.Sales.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Sales.Services;

public interface ISalesService
{
    Task<Result<SaleDto>> CreateSaleAsync(CreateSaleRequest request, CancellationToken ct = default);
    Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<SaleDto>>> GetAllAsync(int page, int pageSize, Guid? branchId, string? status, CancellationToken ct = default);
    Task<Result<SaleDto>> CancelSaleAsync(Guid id, CancellationToken ct = default);
}
