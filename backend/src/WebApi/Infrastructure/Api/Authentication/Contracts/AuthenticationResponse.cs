namespace WebApi.Infrastructure.Api.Authentication.Contracts
{
    internal sealed record AuthenticationResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
}
