using Microsoft.EntityFrameworkCore;

using Xunit;

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

            await postgres.StartAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(options);

            // Act
            await context.Database.MigrateAsync();

            // Assert
            var pendingMigrations =
                await context.Database.GetPendingMigrationsAsync();

            Assert.Empty(pendingMigrations);

            var userCount = await context.Users.CountAsync();

            Assert.Equal(0, userCount);
        }
    }
}
