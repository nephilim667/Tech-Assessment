namespace WebApi.Application.Authentication.Tokens
{
    internal sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
}
