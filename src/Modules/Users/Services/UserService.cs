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
        q = q.Where(u => u.Branch == branchId.Value);

        if(!string.IsNullOrWhiteSpace(role))
        q = q.Where(u => u.Roles.Contains(role));

        if(!string.IsNullOrWhiteSpace(search))
        q = q.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search));

        var totalCount = await q.CountAsync(ct);

        var items = await q.Skip((page -1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Result<PagedResult<UserProfile>>.Ok(
        new(items.Select(ToProfile), page, pageSize, totalCount)
        );
    }

    public async Task<Result<UserProfile>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var q = await Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
        

        return q is null ?  Result<UserProfile>.NotFound("User not found")
                            : Result<UserProfile>.Ok(ToProfile(q));
    }



    public async Task<Result<UserProfile>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var exists = await Users.AnyAsync(u => u.Email == request.Email, ct );
         if(exists)
         return Result<UserProfile>.Fail("User with this email already exists", 409);

        // TODO hash user password with BCrypt finding a way to use 
         var user = new User
         {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            // PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Branch = request.BranchId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            Roles = request.Roles,

            
         };
            
         Users.Add(user);
         await  db.SaveChangesAsync(ct);

        return Result<UserProfile>.Created(ToProfile(user));

    }

    public async Task<Result<UserProfile>>  UpdateAsync( Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
        if(user is null)
        return Result<UserProfile>.NotFound("User not found");

        if(request.FirstName is not null)
            user.FirstName = request.FirstName;

        if(request.LastName is not null)
            user.LastName = request.LastName;

        if(request.Phone is not null)
            user.Phone = request.Phone;

        if(request.Roles is not null)
            user.Roles = request.Roles;

        await db.SaveChangesAsync(ct);

        return Result<UserProfile>.Ok(ToProfile(user));
    }


    public async Task<Result<bool>> AssignRolesAsync(Guid id, AssignRoleRequest request, CancellationToken ct = default)
    {

    }
    private static UserProfile ToProfile(User u)
    => new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Branch, u.IsActive, u.Roles, u.CreatedAt, u.LastLoginAt, u.UpdatedAt);

}