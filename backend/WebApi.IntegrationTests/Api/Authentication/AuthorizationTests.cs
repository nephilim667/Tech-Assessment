using System.Net;
using System.Net.Http.Headers;

using Microsoft.Extensions.DependencyInjection;

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
    }
}
