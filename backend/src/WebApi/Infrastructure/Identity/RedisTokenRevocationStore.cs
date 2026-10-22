using StackExchange.Redis;
using WebApi.Application.Interfaces.Authentication;

namespace WebApi.Infrastructure.Identity
{
    public class RedisTokenRevocationStore(IConnectionMultiplexer connection, TimeProvider timeProvider) : ITokenRevocationStore
    {
        private const string KeyPrefix = "auth:revoked:";

        public async Task RevokeAsync(string tokenId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remainingLifetime = expiresAtUtc - timeProvider.GetUtcNow();

            if (remainingLifetime <= TimeSpan.Zero)
                return;

            var stored = await connection.GetDatabase()
                .StringSetAsync(
                    $"{KeyPrefix}{tokenId}",
                    "1",
                    remainingLifetime)
                .WaitAsync(cancellationToken);

            if (!stored)
                throw new InvalidOperationException("La révocation du token n’a pas été enregistrée.");
        }

        public async Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await connection.GetDatabase()
                .KeyExistsAsync($"{KeyPrefix}{tokenId}")
                .WaitAsync(cancellationToken);
        }
    }
}
