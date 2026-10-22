using WebApi.Domain.Common;
using WebApi.Application.Authentication.Tokens;

namespace WebApi.Application.Interfaces.Authentication
{
    internal interface IAuthenticationProvider
    {
        Task<Result<IssuedAccessToken>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
