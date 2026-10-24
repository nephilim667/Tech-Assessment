using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Domain.Contracts.Expenses;

using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Application.Interfaces.Expenses;

using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Persistance
{
    public sealed class ExpenseReportRepositoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task AddAsync_Does_Not_Persist_Until_UnitOfWork_Saves()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = CreateModels();

            // Le bénéficiaire doit déjà exister.
            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Beneficiaries.Add(user.ToEntity());
                await context.SaveChangesAsync(token);
            }

            using (var operationScope = factory.Services.CreateScope())
            {
                var repository = operationScope.ServiceProvider.GetRequiredService<IExpenseReportRepository>();
                var unitOfWork = operationScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await repository.AddAsync(report, token);

                // Un contexte indépendant ne doit pas encore trouver la note.
                using (var verificationScope = factory.Services.CreateScope())
                {
                    var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

                    Assert.False(await context.ExpenseReports.AnyAsync(entity => entity.Id == report.Id, token));
                }

                await unitOfWork.SaveChangesAsync(token);
            }

            // La note doit être persistée après la sauvegarde.
            using var finalScope = factory.Services.CreateScope();
            var finalContext = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await finalContext.ExpenseReports.AsNoTracking().SingleAsync(entity => entity.Id == report.Id, token);

            Assert.Equal(report.UserId, persisted.UserId);
            Assert.Equal(report.Title, persisted.Title);
            Assert.Equal(report.Year, persisted.Year);
            Assert.Equal(report.Month, persisted.Month);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task GetByIdAsync_Applies_User_Visibility_And_Preserves_Data(bool isActive, bool isDeleted)
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = CreateModels();

            // La note a été créée avant le changement d’état du bénéficiaire.
            if (isDeleted)
                user.Delete();
            else if (!isActive)
                user.Deactivate();

            await PersistAsync(user, report, token);

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<IExpenseReportRepository>();

            var result = await repository.GetByIdAsync(report.Id, token);

            if (isDeleted)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Equal(report.Id, result.Id);
                Assert.Equal(report.UserId, result.UserId);
                Assert.Equal(report.Title, result.Title);
                Assert.Equal(report.Year, result.Year);
                Assert.Equal(report.Month, result.Month);
            }

            // Le filtre de lecture ne doit jamais supprimer la note en base.
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.True(await context.ExpenseReports.AnyAsync(entity => entity.Id == report.Id, token));
        }

        [Fact]
        public async Task GetByIdAsync_When_Report_Does_Not_Exist_Returns_Null()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseReportRepository>();

            var result = await repository.GetByIdAsync(Guid.NewGuid(), token);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUserAndPeriodAsync_Filters_User_Year_And_Month()
        {
            var token = TestContext.Current.CancellationToken;

            var (user, first) = CreateModels();
            var (otherUser, otherUserReport) = CreateModels();

            await PersistAsync(user, first, token);
            await PersistAsync(otherUser, otherUserReport, token);

            var second = ExpenseReport.Restore(Guid.NewGuid(), user.Id, first.Title, 2026, 10);
            var otherMonth = ExpenseReport.Restore(Guid.NewGuid(), user.Id, "Autre mois", 2026, 9);
            var otherYear = ExpenseReport.Restore(Guid.NewGuid(), user.Id, "Autre année", 2025, 10);

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.ExpenseReports.AddRange(
                    second.ToEntity(),
                    otherMonth.ToEntity(),
                    otherYear.ToEntity());

                await context.SaveChangesAsync(token);
            }

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseReportRepository>();

            var result = await repository.GetByUserAndPeriodAsync(user.Id, 2026, 10, token);

            Assert.Equal(2, result.Count);

            Assert.Equal(
                new[] { first.Id, second.Id }.OrderBy(id => id).ToArray(),
                result.Select(report => report.Id).ToArray());

            Assert.All(result, report =>
            {
                Assert.Equal(user.Id, report.UserId);
                Assert.Equal(2026, report.Year);
                Assert.Equal(10, report.Month);
            });
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task GetByUserAndPeriodAsync_Applies_User_Visibility(bool isActive, bool isDeleted)
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = CreateModels();

            if (isDeleted)
                user.Delete();
            else if (!isActive)
                user.Deactivate();

            await PersistAsync(user, report, token);

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseReportRepository>();
            var result = await repository.GetByUserAndPeriodAsync(user.Id, report.Year, report.Month, token);

            if (isDeleted)
            {
                Assert.Empty(result);
            }
            else
            {
                var returned = Assert.Single(result);

                Assert.Equal(report.Id, returned.Id);
                Assert.Equal(report.Title, returned.Title);
                Assert.Equal(report.UserId, returned.UserId);
                Assert.Equal(report.Year, returned.Year);
                Assert.Equal(report.Month, returned.Month);
            }

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.True(await context.ExpenseReports
                .AnyAsync(entity => entity.Id == report.Id, token));
        }

        [Fact]
        public async Task GetByUserAndPeriodAsync_When_User_Does_Not_Exist_Returns_Empty_List()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseReportRepository>();
            var result = await repository.GetByUserAndPeriodAsync(Guid.NewGuid(), 2026, 10, token);

            Assert.Empty(result);
        }

        // Helpers
        private async Task PersistAsync(User user, ExpenseReport report, CancellationToken cancellationToken)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(cancellationToken);

            context.ExpenseReports.Add(report.ToEntity());
            await context.SaveChangesAsync(cancellationToken);
        }

        private static (User User, ExpenseReport Report) CreateModels()
        {
            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            var report = ExpenseReport.Create(user, 2026, 10).Value;

            return (user, report);
        }
    }
}
