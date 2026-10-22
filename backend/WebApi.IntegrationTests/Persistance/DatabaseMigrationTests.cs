using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;
using WebApi.Infrastructure.Persistence;

namespace WebApi.IntegrationTests.Persistance
{
    public sealed class DatabaseMigrationTests
    {
        [Fact]
        public async Task MigrateAsync_When_Database_Is_Empty_Creates_User_Table()
        {
            // Arrange
            await using var postgres = new PostgreSqlBuilder("postgres:17.4")
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(options);

            // Act
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            // Assert
            var pendingMigrations =
                await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Empty(pendingMigrations);

            var userCount = await context.Beneficiaries.CountAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, userCount);

            var accountCount = await context.Set<IdentityUser<Guid>>().CountAsync(TestContext.Current.CancellationToken);
            var roleCount = await context.Set<IdentityRole<Guid>>().CountAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, accountCount);
            Assert.Equal(0, roleCount);
        }
    }
}
