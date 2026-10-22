using System.Net;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Identity.Configuration;
using WebApi.Application.Interfaces.Authentication;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Authentication
{
    public sealed class AuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Theory]
        [InlineData("GET", "/api/users")]
        [InlineData("POST", "/api/users")]
        [InlineData("GET", "/api/users/11111111-1111-1111-1111-111111111111")]
        [InlineData("PUT", "/api/users/11111111-1111-1111-1111-111111111111/status")]
        [InlineData("DELETE", "/api/users/11111111-1111-1111-1111-111111111111")]
        public async Task UserEndpoints_When_Anonymous_Return_Unauthorized(string method, string path)
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), path);

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_When_Token_Is_Invalid_Returns_Unauthorized()
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            using var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_When_Token_Has_No_Allowed_Role_Returns_Forbidden()
        {
            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();
            var token = issuer.Generate(Guid.NewGuid(), []);
            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =new AuthenticationHeaderValue("Bearer", token.AccessToken);

            using var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_When_Token_Is_Revoked_Returns_Unauthorized()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            using var client = await factory.CreateSuperAdminClientAsync(cancellationToken);

            var tokenValue = client.DefaultRequestHeaders.Authorization!.Parameter!;
            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenValue);
            var tokenId = token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

            // Le token est accepté avant révocation.
            using var initialResponse = await client.GetAsync(
                "/api/users",
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);

            using var scope = factory.Services.CreateScope();

            var store = scope.ServiceProvider.GetRequiredService<ITokenRevocationStore>();

            await store.RevokeAsync(tokenId,
                new DateTimeOffset(
                    DateTime.SpecifyKind(token.ValidTo, DateTimeKind.Utc)),
                    cancellationToken);

            // Le même token doit maintenant être refusé.
            using var response = await client.GetAsync(
                "/api/users",
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("expired")]
        [InlineData("invalid_signature")]
        [InlineData("invalid_issuer")]
        [InlineData("invalid_audience")]
        public async Task GetUsers_When_Token_Validation_Fails_Returns_Unauthorized(string failure)
        {
            using var scope = factory.Services.CreateScope();

            var configured = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

            var options = new JwtOptions
            {
                Issuer = failure == "invalid_issuer" ? "untrusted-issuer" : configured.Issuer,
                Audience = failure == "invalid_audience" ? "another-audience" : configured.Audience,
                SigningKey = failure == "invalid_signature" ? new string('X', 64) : configured.SigningKey,

                ExpirationMinutes = configured.ExpirationMinutes
            };

            var now = DateTimeOffset.UtcNow;
            var issuedAt = failure == "expired" ? now.AddMinutes(-configured.ExpirationMinutes - 10) : now;
            var issuer = new JwtTokenIssuer(Options.Create(options), new FixedTimeProvider(issuedAt));
            var token = issuer.Generate(Guid.NewGuid(), ["SuperAdmin"]);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            using var response = await client.GetAsync(
                "/api/users",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("SuperAdmin")]
        public async Task GetUsers_When_Token_Has_Allowed_Role_Returns_Ok(
            string role)
        {
            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();
            var token = issuer.Generate(Guid.NewGuid(), [role]);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            using var response = await client.GetAsync(
                "/api/users",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }
    }
}
