using StackExchange.Redis;
using Testcontainers.Redis;
using DotNet.Testcontainers.Builders;

using WebApi.Infrastructure.Identity;

namespace WebApi.IntegrationTests.Identity
{
    public sealed class RedisTokenRevocationStoreTests
    {
        [Fact]
        public async Task RevokeAsync_Marks_Token_As_Revoked_With_Expiration()
        {
            await using var redis = new RedisBuilder("redis:7.4").Build();
            await redis.StartAsync(TestContext.Current.CancellationToken);

            using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var store = new RedisTokenRevocationStore(connection, TimeProvider.System);

            var cancellationToken = TestContext.Current.CancellationToken;
            var tokenId = Guid.NewGuid().ToString();

            Assert.False(await store.IsRevokedAsync(tokenId, cancellationToken));

            // Act
            await store.RevokeAsync(tokenId, DateTimeOffset.UtcNow.AddMinutes(1), cancellationToken);

            // Assert
            Assert.True(await store.IsRevokedAsync(tokenId, cancellationToken));

            var ttl = await connection.GetDatabase().KeyTimeToLiveAsync($"auth:revoked:{tokenId}");

            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task RevokeAsync_When_Token_Is_Expired_Does_Not_Store_Revocation()
        {
            await using var redis = new RedisBuilder("redis:7.4").Build();
            await redis.StartAsync(TestContext.Current.CancellationToken);

            using var connection = await ConnectionMultiplexer.ConnectAsync(
                redis.GetConnectionString());

            var store = new RedisTokenRevocationStore(connection, TimeProvider.System);

            var tokenId = Guid.NewGuid().ToString();
            var cancellationToken = TestContext.Current.CancellationToken;

            await store.RevokeAsync(tokenId, DateTimeOffset.UtcNow.AddMinutes(-1), cancellationToken);

            Assert.False(await store.IsRevokedAsync(tokenId, cancellationToken));
        }

        [Fact]
        public async Task Revocation_Survives_Container_Recreation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var tokenId = Guid.NewGuid().ToString();
            var expiration = DateTimeOffset.UtcNow.AddMinutes(10);

            await using var volume = new VolumeBuilder().Build();
            await volume.CreateAsync(cancellationToken);

            // Premier conteneur : enregistre la révocation.
            await using (var firstRedis = BuildRedis())
            {
                await firstRedis.StartAsync(cancellationToken);

                using (var connection = await ConnectionMultiplexer.ConnectAsync(firstRedis.GetConnectionString()))
                {
                    var store = new RedisTokenRevocationStore(connection, TimeProvider.System);

                    await store.RevokeAsync(tokenId, expiration, cancellationToken);
                    Assert.True(await store.IsRevokedAsync(tokenId, cancellationToken));
                }

                await firstRedis.StopAsync(cancellationToken);
            }

            // Nouveau conteneur : recharge les données du même volume.
            await using var secondRedis = BuildRedis();
            await secondRedis.StartAsync(cancellationToken);

            using var secondConnection = await ConnectionMultiplexer.ConnectAsync(secondRedis.GetConnectionString());

            var restoredStore = new RedisTokenRevocationStore(secondConnection, TimeProvider.System);

            Assert.True(await restoredStore.IsRevokedAsync(tokenId, cancellationToken));

            var ttl = await secondConnection.GetDatabase()
                .KeyTimeToLiveAsync($"auth:revoked:{tokenId}");

            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, TimeSpan.Zero, TimeSpan.FromMinutes(10));

            RedisContainer BuildRedis()
            {
                return new RedisBuilder("redis:7.4")
                    .WithVolumeMount(volume, "/data")
                    .WithCommand(
                        "redis-server",
                        "--appendonly", "yes",
                        "--appendfsync", "always",
                        "--maxmemory-policy", "noeviction")
                    .Build();
            }
        }
    }
}
