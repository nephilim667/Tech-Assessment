namespace WebApi.Application.Interfaces.Authentication
{
    internal interface ITokenRevocationStore
    {
        Task RevokeAsync(string tokenId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);
        Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
    }
}
