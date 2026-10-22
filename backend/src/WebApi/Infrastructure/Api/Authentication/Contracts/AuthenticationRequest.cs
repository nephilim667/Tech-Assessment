namespace WebApi.Infrastructure.Api.Authentication.Contracts
{
    internal sealed record AuthenticationRequest(string Email, string Password);
}
