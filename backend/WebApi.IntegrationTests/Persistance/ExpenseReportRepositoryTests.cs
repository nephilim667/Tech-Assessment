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

        [Theory]
        [InlineData("active", true)]
        [InlineData("inactive", true)]
        [InlineData("deleted_expense", false)]
        [InlineData("deleted_user", false)]
        public async Task GetByIdAsync_Applies_Visibility_Without_Deleting_Data(string state, bool expectedVisible)
        {
            var token = TestContext.Current.CancellationToken;
            var (expense, _) = await PrepareExpenseAsync(token, state);

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            var result = await repository.GetByIdAsync(expense.Id, token);

            if (expectedVisible)
            {
                Assert.NotNull(result);
                Assert.Equal(expense.Id, result.Id);
                Assert.Equal(expense.ExpenseReportId, result.ExpenseReportId);
                Assert.Equal(expense.Date, result.Date);
                Assert.Equal(expense.Description, result.Description);
                Assert.Equal(expense.Amount, result.Amount);
                Assert.Equal(expense.BillingStreet, result.BillingStreet);
                Assert.Equal(expense.BillingPostalCode, result.BillingPostalCode);
                Assert.Equal(expense.BillingCity, result.BillingCity);
                Assert.Equal(expense.BillingBrand, result.BillingBrand);
                Assert.False(result.IsDeleted);
            }
            else
            {
                Assert.Null(result);
            }

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.True(await context.Expenses
                .AnyAsync(entity => entity.Id == expense.Id, token));
        }

        [Fact]
        public async Task GetByIdAsync_When_Expense_Does_Not_Exist_Returns_Null()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            var result = await repository.GetByIdAsync(Guid.NewGuid(), token);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateStateAsync_After_Save_Soft_Deletes_Expense_And_Frees_Quota()
        {
            var token = TestContext.Current.CancellationToken;
            var (expense, report) = await PrepareExpenseAsync(token);

            expense.Delete();

            using (var operationScope = factory.Services.CreateScope())
            {
                var repository = operationScope.ServiceProvider.GetRequiredService<IExpenseRepository>();
                var unitOfWork = operationScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await repository.UpdateStateAsync(expense, token);

                // Le repository ne sauvegarde pas lui-même.
                using (var verificationScope = factory.Services.CreateScope())
                {
                    var verificationRepository = verificationScope.ServiceProvider.GetRequiredService<IExpenseRepository>();

                    Assert.NotNull(await verificationRepository.GetByIdAsync(expense.Id, token));

                    Assert.Equal(1,
                        await verificationRepository.CountByUserAndMonthAsync(
                            report.UserId, report.Year, report.Month, token));
                }

                await unitOfWork.SaveChangesAsync(token);
            }

            using var finalScope = factory.Services.CreateScope();
            var finalRepository = finalScope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            Assert.Null(await finalRepository.GetByIdAsync(expense.Id, token));

            Assert.Equal(0,
                await finalRepository.CountByUserAndMonthAsync(
                    report.UserId, report.Year, report.Month, token));

            var context = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.True(persisted.IsDeleted);
            Assert.Equal(expense.Amount, persisted.Amount);
            Assert.Equal(expense.Description, persisted.Description);
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

        private async Task<(Expense Expense, ExpenseReport Report)> PrepareExpenseAsync(CancellationToken token, string state = "active")
        {
            var (user, report) = CreateModels();

            var expense = Expense.Create(report,
                new ExpenseDetails(
                    Date: new DateOnly(2026, 10, 15),
                    Description: "Déjeuner",
                    Amount: 25.50m,
                    BillingStreet: "20 rue du Restaurant",
                    BillingPostalCode: "69001",
                    BillingCity: "Lyon")).Value;

            switch (state)
            {
                case "inactive":
                    user.Deactivate();
                    break;
                case "deleted_user":
                    user.Delete();
                    break;
                case "deleted_expense":
                    expense.Delete();
                    break;
            }

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(token);

            context.ExpenseReports.Add(report.ToEntity());
            await context.SaveChangesAsync(token);

            context.Expenses.Add(expense.ToEntity());
            await context.SaveChangesAsync(token);

            return (expense, report);
        }
    }
}
