using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;
using PosSystem.Modules.Users.Entities;
using PosSystem.Modules.Users.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Users.Services;

public class UserService(PosDbContext db): IUserService
{   private DbSet<User> Users => db.Set<User>();
    public  async Task<Result<PagedResult<UserProfile>>> GetResultAsync(
        int page, int pageSize, Guid? branchId, string? role, string search, CancellationToken ct = default)
    {
        var q = Users.Where(u => !u.IsDeleted).AsQueryable(); // loads active users as queryable

        if(branchId.HasValue)
        q = q.Where(u => u.BranchId == branchId.Value);

        if(!string.IsNullOrWhiteSpace(role))
        q = q.Where(u => u.Roles.Contains(role));

        if(!string.IsNullOrWhiteSpace(search))
        q = q.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search));

        var totalCount = await q.CountAsync(ct);

    }
    
}