using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Persistence;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Identity
{
    public sealed class IdentityAccountTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task CreateAccount_Persists_Hashed_Password_And_Role()
        {
            // Arrange
            using var scope = factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            const string roleName = "IntegrationTestRole";
            const string password = "Test-Admin_2026!";

            var account = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "identity-test@example.test",
                Email = "identity-test@example.test"
            };

            // Act
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName
                });
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Code)));

            var createResult = await userManager.CreateAsync(account, password);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(error => error.Code)));

            var assignmentResult = await userManager.AddToRoleAsync(account, roleName);
            Assert.True(assignmentResult.Succeeded, string.Join("; ", assignmentResult.Errors.Select(error => error.Code)));

            // Assert
            Assert.True(await userManager.CheckPasswordAsync(account, password));
            Assert.False(await userManager.CheckPasswordAsync(account, "Incorrect-Password_2026!"));

            Assert.True(await userManager.IsInRoleAsync(account, roleName));

            // Vérification avec un contexte indépendant.
            using var readScope = factory.Services.CreateScope();

            var context = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Users
                .AsNoTracking()
                .SingleAsync(
                    user => user.Id == account.Id,
                    TestContext.Current.CancellationToken);

            Assert.Equal(account.Email, persisted.Email);
            Assert.False(string.IsNullOrWhiteSpace(persisted.PasswordHash));
            Assert.NotEqual(password, persisted.PasswordHash);
        }

        [Fact]
        public async Task SeedRolesAsync_When_Called_Twice_Preserves_Existing_Roles()
        {
            using var scope = factory.Services.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cancellationToken = TestContext.Current.CancellationToken;

            // Première initialisation.
            await seeder.SeedRolesAsync();

            var firstIds = await context.Roles
                .AsNoTracking()
                .Where(role => role.Name == "Admin" || role.Name == "SuperAdmin")
                .OrderBy(role => role.Name)
                .Select(role => role.Id)
                .ToArrayAsync(cancellationToken);

            Assert.Equal(2, firstIds.Length);

            // Nouvelle initialisation.
            await seeder.SeedRolesAsync();

            var secondIds = await context.Roles
                .AsNoTracking()
                .Where(role => role.Name == "Admin" || role.Name == "SuperAdmin")
                .OrderBy(role => role.Name)
                .Select(role => role.Id)
                .ToArrayAsync(cancellationToken);

            Assert.Equal(firstIds, secondIds);
        }

        [Fact]
        public async Task SeedSuperAdminAsync_When_Called_Twice_Preserves_Account()
        {
            using var scope = factory.Services.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await seeder.SeedSuperAdminAsync();

            var firstAccount = await userManager.FindByEmailAsync("superadmin@example.test");

            Assert.NotNull(firstAccount);
            Assert.True(await userManager.IsInRoleAsync(firstAccount, "SuperAdmin"));

            Assert.True(await userManager.CheckPasswordAsync(firstAccount, "Test-SuperAdmin@2026!"));

            var initialId = firstAccount.Id;
            var initialPasswordHash = firstAccount.PasswordHash;
            var initialSecurityStamp = firstAccount.SecurityStamp;

            await seeder.SeedSuperAdminAsync();

            var persisted = await context.Users
                .AsNoTracking()
                .SingleAsync(user => user.Id == initialId, TestContext.Current.CancellationToken);

            Assert.Equal(initialPasswordHash, persisted.PasswordHash);
            Assert.Equal(initialSecurityStamp, persisted.SecurityStamp);

            Assert.Equal(1,
                await context.Users.CountAsync(
                    user => user.NormalizedEmail == "SUPERADMIN@EXAMPLE.TEST",
                    TestContext.Current.CancellationToken));
        }
    }
}
