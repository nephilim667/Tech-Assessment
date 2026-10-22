using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Infrastructure.Identity;
using WebApi.IntegrationTests.Infrastructure;
using WebApi.Application.Interfaces.Authentication;

namespace WebApi.IntegrationTests.Identity
{
    public sealed class AuthenticationProviderTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task LoginAsync_When_Credentials_Are_Valid_Returns_SuperAdmin_Token()
        {
            using var scope = factory.Services.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await seeder.SeedSuperAdminAsync();

            var service = scope.ServiceProvider.GetRequiredService<IAuthenticationProvider>();

            // Act
            var result = await service.AuthenticateAsync("superadmin@example.test", "Test-SuperAdmin@2026!", TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.IsSuccess);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);

            Assert.Contains(token.Claims, claim => claim.Type == "role" && claim.Value == "SuperAdmin");
            Assert.Contains(token.Claims, claim =>
                claim.Type == JwtRegisteredClaimNames.Sub
                && Guid.TryParse(claim.Value, out _));

            Assert.True(result.Value.ExpiresAtUtc > DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData("superadmin@example.test", "Incorrect-Password_2026!")]
        [InlineData("missing@example.test", "Test-SuperAdmin@2026!")]
        public async Task LoginAsync_When_Credentials_Are_Invalid_Returns_Generic_Error(string email, string password)
        {
            using var scope = factory.Services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedSuperAdminAsync();

            var service = scope.ServiceProvider.GetRequiredService<IAuthenticationProvider>();

            var result = await service.AuthenticateAsync(email, password, TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal("auth.invalid_credentials", result.Error.Code);
        }
    }
}
