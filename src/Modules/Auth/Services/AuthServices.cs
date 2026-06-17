using Microsoft.EntityFrameworkCore;
using PosSystem.Infrastructure;
using PosSystem.Modules.Auth.Entities;
using PosSystem.Modules.Auth.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Auth.Services;

public class AuthService(PosDbContext db, TokenService tokenService) : IAuthService
{
    private DbSet<AppUser> Users => db.Set<AppUser>();

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 1. Find user by email
        var user = await Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, ct);
        if (user is null)
            return Result<LoginResponse>.Unauthorized("Invalid email or password");

        // 2. Verify password hash
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Unauthorized("Invalid email or password");

        // 3. Check if account is active
        if (!user.IsActive)
            return Result<LoginResponse>.Fail("Account is deactivated. Contact your administrator.", 403);

        // 4. Generate tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        // 5. Store refresh token in DB
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = tokenService.GetRefreshTokenExpiry();
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // 6. Return response
        return Result<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(480),
            ToProfile(user)
        ));
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Find user by refresh token
        var user = await Users.FirstOrDefaultAsync(
            u => u.RefreshToken == refreshToken && !u.IsDeleted, ct);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<LoginResponse>.Unauthorized("Invalid or expired refresh token");

        // Generate new token pair
        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        // Rotate the refresh token (old one is now invalid)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = tokenService.GetRefreshTokenExpiry();
        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Ok(new LoginResponse(
            newAccessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(480),
            ToProfile(user)
        ));
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var user = await Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);
        if (user is null) return Result<bool>.Ok(true); // already logged out

        // Clear the refresh token so it can't be reused
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
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
        var user = await Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
        return user is null
            ? Result<UserProfile>.NotFound("User not found")
            : Result<UserProfile>.Ok(ToProfile(user));
    }

    private static UserProfile ToProfile(AppUser u) => new(
        u.Id, u.FirstName, u.LastName, u.Email, u.Branch, u.Roles);
}