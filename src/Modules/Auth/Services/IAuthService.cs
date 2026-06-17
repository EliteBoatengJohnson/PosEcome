using PosSystem.Modules.Auth.Models;
using PosSystem.SharedKernel;

namespace PosSystem.Modules.Auth.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(PasswordResetConfirm request, CancellationToken ct = default);
    Task<Result<UserProfile>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}