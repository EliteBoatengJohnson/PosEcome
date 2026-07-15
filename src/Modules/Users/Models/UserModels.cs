namespace PosSystem.Modules.Users.Models;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    Guid BranchId,
    List<string> Roles
);


public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    List<string>? Roles

);

public record AssignRoleRequest(List<string> Roles);

public record TransferBranchRequest(Guid BranchId);

public record UserProfile(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid BranchId,
    bool IsActive,
    List<string> Roles,
    DateTime? CreatedAt,
    DateTime? LastLoginAt,
    DateTime? UpdatedAt
);