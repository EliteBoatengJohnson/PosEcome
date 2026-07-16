using PosSystem.Modules.Branches.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Branches.Services;
public interface IBranchService
{
    Task<Result<List<BranchProfile>>> GetAllAsync(CreateBranchRequest req, CancellationToken ct =default);
     Task<Result<BranchProfile>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<BranchProfile>> CreateAsync(CreateBranchRequest req, CancellationToken ct = default);
    Task<Result<BranchProfile>> UpdateAsync(Guid id, updateBranchRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeactivateAsync(Guid id, CancellationToken ct = default);
    
}