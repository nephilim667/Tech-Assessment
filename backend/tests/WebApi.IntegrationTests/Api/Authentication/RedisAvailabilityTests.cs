using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using StackExchange.Redis;

using WebApi.Application.Interfaces.Authentication;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Authentication
{
    public sealed class RedisAvailabilityTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Theory]
        [InlineData(true, "GET", "/api/users")]
        [InlineData(false, "POST", "/api/auth/logout")]
        public async Task Redis_Failure_Returns_ServiceUnavailable(bool failOnRead, string method, string path)
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            // Obtient un vrai JWT avec l’application et Redis opérationnels.
            using var authenticatedClient = await factory.CreateSuperAdminClientAsync(cancellationToken);

            // Héberge la même API avec une défaillance contrôlée du stockage.
            using var unavailableFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ITokenRevocationStore>();
                    services.AddSingleton<ITokenRevocationStore>(new UnavailableRevocationStore(failOnRead));
                });
            });

            using var client = unavailableFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = authenticatedClient.DefaultRequestHeaders.Authorization;

            using var request = new HttpRequestMessage(new HttpMethod(method), path);

            // Act
            using var response = await client.SendAsync(request, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            Assert.Equal("infrastructure.redis_unavailable", body.RootElement.GetProperty("code").GetString());

            // L’échec ne doit pas être présenté comme une révocation réussie.
            using var originalResponse = await authenticatedClient.GetAsync(
                "/api/users",
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);
        }

        private sealed class UnavailableRevocationStore(bool failOnRead) : ITokenRevocationStore
        {
            public Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
            {
                return failOnRead
                    ? Task.FromException<bool>(CreateException())
                    : Task.FromResult(false);
            }

            public Task RevokeAsync(string tokenId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
            {
                return Task.FromException(CreateException());
            }

            private static RedisConnectionException CreateException()
            {
                return new RedisConnectionException(
                    ConnectionFailureType.UnableToConnect,
                    "Indisponibilité simulée pour le test.");
            }
        }
    }
}
