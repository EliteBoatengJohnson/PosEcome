
using PosSystem.SharedKernel;


namespace PosSystem.Modules.Auth.Entities;

public  class AppUser: BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry{ get; set; }
}