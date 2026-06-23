namespace PosSystem.Modules.Auth.Models;

public record LoginRequest( string Email, string Password);
public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    Guid BranchId,
    List<string>? Roles);
public record RefreshTokens(string RefreshToken);
public record  PasswordResetRequest(string Email);
public record PasswordResetConfirm(string Token, string NewPassword);

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserProfile User);

public record UserProfile(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid BranchId,
    List<string> Roles);