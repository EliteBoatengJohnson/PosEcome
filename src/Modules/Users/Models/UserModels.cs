namespace PosSystem.Modules.Users.Models;

public record UserProfile(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid BranchId,
    List<string> Roles
);

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid Branch

);