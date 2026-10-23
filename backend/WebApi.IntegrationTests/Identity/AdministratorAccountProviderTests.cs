using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Persistence;
using WebApi.IntegrationTests.Infrastructure;
using WebApi.Application.Interfaces.Authentication;

namespace WebApi.IntegrationTests.Identity
{
    public sealed class AdministratorAccountProviderTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task CreateAsync_Creates_Admin_Without_SuperAdmin_Role()
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedRolesAsync();

            var provider = scope.ServiceProvider.GetRequiredService<IAdministratorAccountProvider>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

            // Act
            var result = await provider.CreateAsync("account-creation@example.test", "Test-Administrator_2026!", TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.IsSuccess);

            var account = await userManager.FindByIdAsync(result.Value.Id.ToString());

            Assert.NotNull(account);
            Assert.Equal("account-creation@example.test", account.Email);
            Assert.True(await userManager.IsInRoleAsync(account, "Admin"));
            Assert.False(await userManager.IsInRoleAsync(account, "SuperAdmin"));

            Assert.True(await userManager.CheckPasswordAsync(account, "Test-Administrator_2026!"));
        }

        [Fact]
        public async Task CreateAsync_When_Email_Is_Already_Used_Returns_Conflict()
        {
            using var scope = factory.Services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedRolesAsync();

            var provider = scope.ServiceProvider.GetRequiredService<IAdministratorAccountProvider>();

            var cancellationToken = TestContext.Current.CancellationToken;

            var first = await provider.CreateAsync("duplicate-admin@example.test", "Test-Administrator_2026!", cancellationToken);

            Assert.True(first.IsSuccess);

            // Act : la casse ne doit pas contourner l’unicité.
            var second = await provider.CreateAsync("DUPLICATE-ADMIN@example.test", "Another-Password_2026!", cancellationToken);

            // Assert
            Assert.False(second.IsSuccess);
            Assert.Equal("admin.email_already_used", second.Error.Code);

            using var readScope = factory.Services.CreateScope();

            var context = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Equal(1, await context.Users.CountAsync(
                        account => account.NormalizedEmail == "DUPLICATE-ADMIN@EXAMPLE.TEST", cancellationToken));
        }
    }
}
