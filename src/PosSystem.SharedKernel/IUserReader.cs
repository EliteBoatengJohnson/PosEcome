namespace PosSystem.SharedKernel;

// Synchronous Inter-Module Communication Contract
// This interface lives in SharedKernel so modules can communicate
// without referecing each other directly.

// HOW IT WORKS:
// 1. SharedKernel defines IUserReader (this file)
// 2. UserModule Implements it in UserReaderService.cs
// 3. AuthModule injects IUserReader via constructor DI
// 4. Program.cs registers: services.AddScoped<IUserReader, UserReaderService>();

// RESULT:
// Auth.csproj -> SharedKernel (for IUserReader)
// Users.csproj -> SharedKernel (to implement IUserReader)
// Auth.csproj -> Users.csproj (No direct reference - boundary intact)
public interface IUserReader
{
    // Used by AuthService: check if account is still active before issuing token    
    Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    // Used by AuthService: quick existence check without loading full entity
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);

    // Used by AuthService: get active status without loading full entity
    Task<bool> IsActiveAsync(Guid userId, CancellationToken ct = default);
}

// Lightweight DTO - only What other modules need to know
public record UserSummary(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid BranchId,
    bool IsActive,
    List<string> Roles
);