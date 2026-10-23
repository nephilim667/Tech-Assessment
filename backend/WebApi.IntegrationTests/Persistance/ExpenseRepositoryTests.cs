using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Domain.Contracts.Expenses;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Expenses;

using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Entities;
using WebApi.Infrastructure.Persistence.Mappings;

using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Persistance
{
    public sealed class ExpenseRepositoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task CountByUserAndMonthAsync_Counts_Only_NonDeleted_Expenses_Across_All_Matching_Reports()
        {
            var token = TestContext.Current.CancellationToken;

            var user = CreateUser();
            var otherUser = CreateUser();

            var firstReport = ExpenseReport.Create(user, 2026, 10).Value;
            var secondReport = ExpenseReport.Create(user, 2026, 10).Value;
            var previousMonth = ExpenseReport.Create(user, 2026, 9).Value;
            var previousYear = ExpenseReport.Create(user, 2025, 10).Value;
            var otherUserReport = ExpenseReport.Create(otherUser, 2026, 10).Value;

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Beneficiaries.AddRange(user.ToEntity(), otherUser.ToEntity());

                await context.SaveChangesAsync(token);

                context.ExpenseReports.AddRange(
                    firstReport.ToEntity(),
                    secondReport.ToEntity(),
                    previousMonth.ToEntity(),
                    previousYear.ToEntity(),
                    otherUserReport.ToEntity());

                await context.SaveChangesAsync(token);

                context.Expenses.AddRange(
                    CreateExpenseEntity(firstReport),
                    CreateExpenseEntity(firstReport, isDeleted: true),
                    CreateExpenseEntity(secondReport),
                    CreateExpenseEntity(secondReport),
                    CreateExpenseEntity(previousMonth),
                    CreateExpenseEntity(previousYear),
                    CreateExpenseEntity(otherUserReport));

                await context.SaveChangesAsync(token);
            }

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            Assert.Equal(3, await repository.CountByUserAndMonthAsync(user.Id, 2026, 10, token));
            Assert.Equal(1, await repository.CountByUserAndMonthAsync(user.Id, 2026, 9, token));
            Assert.Equal(1, await repository.CountByUserAndMonthAsync(user.Id, 2025, 10, token));
            Assert.Equal(0, await repository.CountByUserAndMonthAsync(user.Id, 2026, 11, token));
        }

        [Fact]
        public async Task AddAsync_Does_Not_Persist_Until_UnitOfWork_Saves()
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            var report = ExpenseReport.Create(user, 2026, 10).Value;

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Beneficiaries.Add(user.ToEntity());
                await context.SaveChangesAsync(token);

                context.ExpenseReports.Add(report.ToEntity());
                await context.SaveChangesAsync(token);
            }

            var expense = Expense.Create(
                report,
                new ExpenseDetails(
                    Date: new DateOnly(2026, 10, 15),
                    Description: "Déjeuner",
                    Amount: 25.50m,
                    BillingStreet: "20 rue du Restaurant",
                    BillingPostalCode: "69001",
                    BillingCity: "Lyon")).Value;

            using (var operationScope = factory.Services.CreateScope())
            {
                var repository = operationScope.ServiceProvider.GetRequiredService<IExpenseRepository>();
                var unitOfWork = operationScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await repository.AddAsync(expense, token);

                using (var verificationScope = factory.Services.CreateScope())
                {
                    var context = verificationScope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                    Assert.False(await context.Expenses
                        .AnyAsync(entity => entity.Id == expense.Id, token));
                }

                await unitOfWork.SaveChangesAsync(token);
            }

            using var finalScope = factory.Services.CreateScope();
            var finalContext = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await finalContext.Expenses.AsNoTracking().SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.Equal(expense.ExpenseReportId, persisted.ExpenseReportId);
            Assert.Equal(expense.Amount, persisted.Amount);
        }

        // Helpers
        private static User CreateUser() =>
            User.Create(
                "Juste", "Leblanc",
                "10 rue des Lilas", "75001", "Paris", 5).Value;

        private static ExpenseEntity CreateExpenseEntity(ExpenseReport report, bool isDeleted = false) =>
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseReportId = report.Id,
                Date = new DateOnly(report.Year, report.Month, 15),
                Description = "Déjeuner",
                Amount = 25.50m,
                BillingStreet = "20 rue du Restaurant",
                BillingPostalCode = "69001",
                BillingCity = "Lyon",
                IsDeleted = isDeleted
            };
    }
}
