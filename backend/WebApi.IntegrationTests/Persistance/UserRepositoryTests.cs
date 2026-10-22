using Microsoft.EntityFrameworkCore;

using Npgsql;
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

            await postgres.StartAsync(TestContext.Current.CancellationToken);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(options);
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            var repository = new UserRepository(context);

            // Act
            await repository.AddAsync(user, TestContext.Current.CancellationToken);

            // Assert: l'ajout n'a pas encore été sauvegardé.
            Assert.False(await context.Users.AnyAsync(TestContext.Current.CancellationToken));

            var unitOfWork = new UnitOfWork(context);
            await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Lecture indépendante du tracking du contexte d’écriture.
            await using var readContext = new AppDbContext(options);

            var entity = await readContext.Users
                .AsNoTracking()
                .SingleAsync(TestContext.Current.CancellationToken);

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

        [Fact]
        public async Task UpdateStateAsync_Persists_State_Without_Changing_Profile()
        {
            // Arrange
            await using var postgres = new PostgreSqlBuilder("postgres:17.4")
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            var cancellationToken = TestContext.Current.CancellationToken;

            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            await using (var context = new AppDbContext(options))
            {
                await context.Database.MigrateAsync(cancellationToken);

                var repository = new UserRepository(context);
                var unitOfWork = new UnitOfWork(context);

                await repository.AddAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Act : nouveau contexte pour la modification.
            await using (var context = new AppDbContext(options))
            {
                var repository = new UserRepository(context);
                var unitOfWork = new UnitOfWork(context);

                var loaded = await repository.GetByIdAsync(
                    user.Id,
                    cancellationToken);

                Assert.NotNull(loaded);
                Assert.True(loaded.Deactivate().IsSuccess);

                await repository.UpdateStateAsync(loaded, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Assert : nouvelle lecture indépendante.
            await using var readContext = new AppDbContext(options);

            var persisted = await readContext.Users
                .AsNoTracking()
                .SingleAsync(
                    entity => entity.Id == user.Id,
                    cancellationToken);

            Assert.False(persisted.IsActive);
            Assert.False(persisted.IsDeleted);
            Assert.Equal(user.FirstName, persisted.FirstName);
            Assert.Equal(user.LastName, persisted.LastName);
            Assert.Equal(user.Street, persisted.Street);
            Assert.Equal(user.PostalCode, persisted.PostalCode);
            Assert.Equal(user.City, persisted.City);
            Assert.Equal(user.MaxExpensesPerMonth, persisted.MaxExpensesPerMonth);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_When_Operation_Fails_Rolls_Back()
        {
            // Arrange
            await using var postgres = new PostgreSqlBuilder("postgres:17.4")
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            var cancellationToken = TestContext.Current.CancellationToken;

            await using var context = new AppDbContext(options);
            await context.Database.MigrateAsync(cancellationToken);

            var repository = new UserRepository(context);
            var unitOfWork = new UnitOfWork(context);

            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await unitOfWork.ExecuteInTransactionAsync<bool>(
                        async token =>
                        {
                            await repository.AddAsync(user, token);
                            await unitOfWork.SaveChangesAsync(token);

                            throw new InvalidOperationException("Échec simulé.");
                        },
                        cancellationToken);
                });

            // Assert
            Assert.Equal("Échec simulé.", exception.Message);

            await using var readContext = new AppDbContext(options);

            Assert.False(await readContext.Users.AnyAsync(
                entity => entity.Id == user.Id,
                cancellationToken));
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Locks_User_Until_Transaction_Ends()
        {
            // Arrange
            await using var postgres = new PostgreSqlBuilder("postgres:17.4")
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(postgres.GetConnectionString())
                .Options;

            var cancellationToken = TestContext.Current.CancellationToken;

            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            await using (var setupContext = new AppDbContext(options))
            {
                await setupContext.Database.MigrateAsync(cancellationToken);

                var repository = new UserRepository(setupContext);
                var unitOfWork = new UnitOfWork(setupContext);

                await repository.AddAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await using var firstContext = new AppDbContext(options);
            await using var secondContext = new AppDbContext(options);

            await using var firstTransaction =
                await firstContext.Database.BeginTransactionAsync(cancellationToken);

            await using var secondTransaction =
                await secondContext.Database.BeginTransactionAsync(cancellationToken);

            var firstRepository = new UserRepository(firstContext);

            // Act
            var lockedUser = await firstRepository.GetByIdForUpdateAsync(user.Id, cancellationToken);

            // Assert
            Assert.NotNull(lockedUser);
            Assert.Equal(user.Id, lockedUser.Id);

            // NOWAIT permet de vérifier le verrou sans attente arbitraire.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await secondContext.Users
                        .FromSqlInterpolated($"""
                            SELECT *
                            FROM users
                            WHERE "Id" = {user.Id}
                            FOR UPDATE NOWAIT
                            """)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                });

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.LockNotAvailable, postgresException.SqlState);
        }
    }
}
