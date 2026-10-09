// AuthService - Authentication and session management.
//
// CROSS-MODULE COMMUNICATION:
// AuthService needs to check if a user account is active/exists.
// It does NOT reference Users.csproj directly.
// Instead it injects IUserReader (defined in SharedKernel),
// which is implemented by UserModule.UserReaderService.
// Program.cs wires: services.AddScoped<IUserReader, UserReaderService>();
// Auth.csproj -> SharedKernel (IUserReader, Result<T>)
// Auth.csproj -> Users.csproj (No direct reference)

using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;
using PosSystem.Modules.Auth.Entities;
using PosSystem.Modules.Auth.Models;
using PosSystem.Modules.Auth.Services;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Auth.Services;

public class AuthService(PosDbContext db, TokenService tokenService, IUserReader userReader) : IAuthService
{
    private DbSet<AppUser> Users => db.Set<AppUser>();
    // ── Login 
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 1. Find user by email
        var user = await Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, ct);
        if (user is null)
            return Result<LoginResponse>.Unauthorized("Invalid email or password");

        // 2. Verify password hash
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Unauthorized("Invalid email or password");
        
        // 3. Cross-module check via IUserReader(implemented by UserModule)
        // Ask the users module: is this account still active?
        // This call goes through DI - no direct Users.csproj reference.
        var profile = await userReader.GetByIdAsync(user.Id, ct);
        if(profile is null || !profile.IsActive)
            return Result<LoginResponse>.Fail("Account has been deactivated or not found", 403);

    
       var userProfile = ToProfile(profile);

        //4 Issue tokens
        var accessToken = tokenService.GenerateAccessToken(userProfile);
        var refreshToken = tokenService.GenerateRefreshToken();

        // 5. Store refresh token in DB
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = tokenService.GetRefreshTokenExpiry();
        //user.LastLoginAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // return the login response using the fetched profile from 
        return Result<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(480),
            userProfile
        ));
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Find user by refresh token
        var authUser = await Users.FirstOrDefaultAsync(
            u => u.RefreshToken == refreshToken && !u.IsDeleted, ct);

        if (authUser is null || authUser.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<LoginResponse>.Unauthorized("Invalid or expired refresh token");

        // Cross-module check: is the account still active?
        var profile = await userReader.GetByIdAsync(authUser.Id, ct);
        if(profile is null || !profile.IsActive)
            return Result<LoginResponse>.Unauthorized("Account has been deactivated");

        var authUserProfile = ToProfile(profile);
        

        // Generate new token pair
        var newAccessToken = tokenService.GenerateAccessToken(authUserProfile);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        // Rotate the refresh token (old one is now invalid)
        authUser.RefreshToken = newRefreshToken;
        authUser.RefreshTokenExpiry = tokenService.GetRefreshTokenExpiry();
        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Ok(new LoginResponse(
            newAccessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(480),
            authUserProfile
        ));
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var authUser = await Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);
        if (authUser is null) return Result<bool>.Ok(true); // already logged out

        // Clear the refresh token so it can't be reused
        authUser.RefreshToken = null;
        authUser.RefreshTokenExpiry = null;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public Task<Result<bool>> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        // TODO: generate a reset token, store it, and send via email/SMS
        return Task.FromResult(Result<bool>.Ok(true));
    }

    public Task<Result<bool>> ResetPasswordAsync(PasswordResetConfirm request, CancellationToken ct = default)
    {
        // TODO: validate reset token, hash new password, update user
        return Task.FromResult(Result<bool>.Ok(true));
    }

    public async Task<Result<UserProfile>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Use IUserReader to get the full profile from UserModule
    var profile = await userReader.GetByIdAsync(userId, ct);
        var userProfile = ToProfile(profile);
        return profile is null ? Result<UserProfile>.NotFound("user not found")
        : Result<UserProfile>.Ok(userProfile);
    }

    private static UserProfile ToProfile(UserSummary u) => new(
        u.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Phone,
        u.BranchId,
        u.Roles);
}