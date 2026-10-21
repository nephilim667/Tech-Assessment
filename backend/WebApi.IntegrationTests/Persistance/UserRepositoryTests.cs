using Microsoft.EntityFrameworkCore;

using Xunit;
using Testcontainers.PostgreSql;

using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Common;
using WebApi.Infrastructure.Persistence.Repositories;


namespace WebApi.IntegrationTests.Persistance
{
    public sealed class UserRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_User_Only_After_SaveChanges()
        {
            // Arrange
            await using var postgres = new PostgreSqlBuilder("postgres:17.4")
                .Build();

            await postgres.StartAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(options);
            await context.Database.MigrateAsync();

            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            var repository = new UserRepository(context);

            // Act
            await repository.AddAsync(user);

            // Assert: l'ajout n'a pas encore été sauvegardé.
            Assert.False(await context.Users.AnyAsync());

            var unitOfWork = new UnitOfWork(context);
            await unitOfWork.SaveChangesAsync();

            // Lecture indépendante du tracking du contexte d’écriture.
            await using var readContext = new AppDbContext(options);

            var entity = await readContext.Users
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(user.Id, entity.Id);
            Assert.Equal(user.FirstName, entity.FirstName);
            Assert.Equal(user.LastName, entity.LastName);
            Assert.Equal(user.Street, entity.Street);
            Assert.Equal(user.PostalCode, entity.PostalCode);
            Assert.Equal(user.City, entity.City);
            Assert.Equal(user.MaxExpensesPerMonth, entity.MaxExpensesPerMonth);
            Assert.True(entity.IsActive);
            Assert.False(entity.IsDeleted);
        }
    }
}
