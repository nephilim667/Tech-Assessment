using WebApi.Domain.Common;
using WebApi.Application.Authentication.Contrats;

namespace WebApi.Application.Interfaces.Authentication
{
    internal interface IAdministratorAccountProvider
    {
        Task<Result<CreatedAdministratorAccount>> CreateAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
