using System;
using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;     
using PosSystem.Modules.Users.Entities;
using PosSystem.SharedKernel;
namespace PosSystem.Modules.Users.Services;

public class UserReaderService(PosDbContext context) : IUserReader
{
    private DbSet<User> Users => context.Set<User>();

    public async Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await Users.AsNoTracking().Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserSummary(
                u.Id,
                u.FirstName,            
                u.LastName,
                u.Email,
                u.Phone,
                u.Branch,
                u.IsActive,
                u.Roles))           
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return await Users.AsNoTracking().AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);
    }


    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken ct = default)
    {
        return await Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct);
    }

}
