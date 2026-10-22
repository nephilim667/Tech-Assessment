using WebApi.Application.Authentication.Tokens;

namespace WebApi.Application.Interfaces.Authentication
{
    internal interface IAccessTokenIssuer
    {
        IssuedAccessToken Generate(Guid accountId, IReadOnlyCollection<string> roles);
    }
}
