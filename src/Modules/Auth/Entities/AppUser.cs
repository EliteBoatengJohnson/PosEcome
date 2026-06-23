
using PosSystem.SharedKernel;


namespace PosSystem.Modules.Auth.Entities;

public  class AppUser: BaseEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string?  Phone { get; set; }
    public Guid Branch {get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry{ get; set; }
    public List<string> Roles { get; set; } = [];
}