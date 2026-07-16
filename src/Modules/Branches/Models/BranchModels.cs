namespace PosSystem.Modules.Branches.Models;


public record CreateBranchRequest(
    string Name, string Code, string? Address,
    string? Phone, string? Email, string? ManagerName, bool IsHeadOffice
);

public record updateBranchRequest(
    string? Name, string? Address,
    string? Phone, string? ManagerName

);

public record BranchProfile(
    Guid Id, string Name, string Code,
    string? Address, string Phone, string? Email,
    string? ManagerName, bool IsActive, bool IsHeadOffice, DateTime CreatedAt
);