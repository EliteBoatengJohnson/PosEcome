using PosSystem.Modules.Users.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Users.Services;

public interface IUserService
{
    Task<Result<PagedResult<UserProfile>>> GetAllAsync(int page, int pageSize, Guid? branchId, string? role, string search, CancellationToken ct = default);
    Task<Result<UserProfile>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserProfile>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserProfile>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result<bool>> AssignRolesAsync(Guid id, AssignRoleRequest request, CancellationToken ct = default);
    Task<Result<bool>> TransferBranchAsync(Guid id, TransferBranchRequest request, CancellationToken ct = default);
    Task<Result<bool>> ReactivateUserAsync(Guid id, CancellationToken ct = default);
   
    Task<Result<bool>> DeactivateUserAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
   // Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default);
}