using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

using WebApi.Infrastructure.Api.Administrators.Contracts;
using WebApi.Infrastructure.Api.Authentication.Contracts;

using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Administrators
{
    public sealed class AdministratorAccountEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Create_When_Anonymous_Returns_Unauthorized()
        {
            using var client = factory.CreateClient();

            using var response = await client.PostAsJsonAsync("/api/admin-accounts",
                new CreateAdministratorAccountRequest(
                    "anonymous-attempt@example.test",
                    "Test-Administrator_2026!"),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_SuperAdmin_Creates_Only_Admin_Role()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            using var client = await factory.CreateSuperAdminClientAsync(cancellationToken);

            // Le rôle supplémentaire n’appartient pas au contrat.
            using var response = await client.PostAsJsonAsync(
                "/api/admin-accounts",
                new
                {
                    Email = "http-created-admin@example.test",
                    Password = "Test-Administrator_2026!",
                    Role = "SuperAdmin"
                },
                cancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var account = await response.Content.ReadFromJsonAsync<AdministratorAccountResponse>(cancellationToken);

            Assert.NotNull(account);
            Assert.NotEqual(Guid.Empty, account.Id);

            using var anonymousClient = factory.CreateClient();
            using var authenticationResponse = await anonymousClient.PostAsJsonAsync(
                "/api/auth/login",
                new AuthenticationRequest(
                    account.Email,
                    "Test-Administrator_2026!"),
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, authenticationResponse.StatusCode);

            var authentication = await authenticationResponse.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);

            Assert.NotNull(authentication);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(authentication.AccessToken);

            Assert.Contains(token.Claims, claim =>claim.Type == "role" && claim.Value == "Admin");
            Assert.DoesNotContain(token.Claims, claim => claim.Type == "role" && claim.Value == "SuperAdmin");

            // Ce compte peut gérer les bénéficiaires.
            anonymousClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
            using var usersResponse = await anonymousClient.GetAsync("/api/users", cancellationToken);

            Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

            // Il ne peut pas créer un autre compte administrateur.
            using var forbiddenResponse = await anonymousClient.PostAsJsonAsync(
                "/api/admin-accounts",
                new CreateAdministratorAccountRequest(
                    "forbidden-attempt@example.test",
                    "Test-Administrator_2026!"),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        }

        [Fact]
        public async Task Create_When_Request_Is_Invalid_Returns_BadRequest()
        {
            using var client = await factory.CreateSuperAdminClientAsync(TestContext.Current.CancellationToken);

            using var response = await client.PostAsJsonAsync(
                "/api/admin-accounts",
                new CreateAdministratorAccountRequest("", "short"),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
