using PosSystem.SharedKernel;

namespace PosSystem.SharedKernel.Interfaces;

public  interface IAuthProvisioner
{
    Task<Result<bool>> CreateCredentialsAsync(Guid userid, string email ,string plainTextPassword, CancellationToken ct = default);
}