using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApi.Infrastructure.Api.Authentication.Contracts;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Identity.Configuration;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Authentication
{
    public sealed class AuthenticationEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Login_When_Credentials_Are_Valid_Returns_Bearer_Token()
        {
            using var scope = factory.Services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedSuperAdminAsync();

            var credentials = scope.ServiceProvider
                .GetRequiredService<IOptions<BootstrapAdminOptions>>()
                .Value;

            using var client = factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            using var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new AuthenticationRequest(credentials.Email, credentials.Password),
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);

            Assert.NotNull(body);
            Assert.Equal("Bearer", body.TokenType);
            Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
            Assert.True(body.ExpiresAtUtc > DateTimeOffset.UtcNow);
            Assert.False(response.Headers.Contains("Set-Cookie"));

            var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);

            Assert.Contains(token.Claims, claim => claim.Type == "role" && claim.Value == "SuperAdmin");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Login_When_Credentials_Are_Invalid_Returns_Unauthorized(
            bool accountExists)
        {
            using var scope = factory.Services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedSuperAdminAsync();

            var credentials = scope.ServiceProvider
                .GetRequiredService<IOptions<BootstrapAdminOptions>>()
                .Value;

            var request = new AuthenticationRequest(
                accountExists ? credentials.Email : "missing@example.test",
                accountExists ? "Incorrect-Password_2026!" : credentials.Password);

            using var client = factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            using var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                request,
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            Assert.Equal("auth.invalid_credentials", body.RootElement.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Login_When_Fields_Are_Empty_Returns_ValidationErrors()
        {
            using var client = factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            using var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new AuthenticationRequest("", ""),
                cancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            var errors = body.RootElement.GetProperty("errors");

            Assert.True(errors.GetProperty("Email").GetArrayLength() > 0);
            Assert.True(errors.GetProperty("Password").GetArrayLength() > 0);
        }
    }
}
