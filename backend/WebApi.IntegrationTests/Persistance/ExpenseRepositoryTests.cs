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

        [Fact]
        public async Task GetPageAsync_Paginates_By_Five_And_Returns_Global_Total()
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();

            var report = ExpenseReport.Create(user, 2026, 10).Value;
            var otherReport = ExpenseReport.Create(user, 2026, 10).Value;

            // Identifiants triés pour vérifier explicitement le départage
            // des dépenses qui ont la même date.
            var ids = Enumerable.Range(0, 7)
                .Select(_ => Guid.NewGuid())
                .OrderBy(id => id)
                .ToArray();

            var expenses = ids.Select((id, index) =>
            {
                var entity = CreateExpenseEntity(report);
                entity.Id = id;
                entity.Amount = (index + 1) * 10m;
                return entity;
            }).ToArray();

            // Cette dépense doit apparaître avant toutes les autres.
            expenses[6].Date = new DateOnly(2026, 10, 16);

            var deleted = CreateExpenseEntity(report, isDeleted: true);
            deleted.Amount = 1000m;

            var unrelated = CreateExpenseEntity(otherReport);
            unrelated.Amount = 9000m;

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Beneficiaries.Add(user.ToEntity());
                await context.SaveChangesAsync(token);

                context.ExpenseReports.AddRange(
                    report.ToEntity(),
                    otherReport.ToEntity());

                await context.SaveChangesAsync(token);

                context.Expenses.AddRange(expenses);
                context.Expenses.AddRange(deleted, unrelated);

                await context.SaveChangesAsync(token);
            }

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            var first = await repository.GetPageAsync(report.Id, 1, token);
            var second = await repository.GetPageAsync(report.Id, 2, token);
            var beyondLast = await repository.GetPageAsync(report.Id, 3, token);
            var veryLargePage = await repository.GetPageAsync(report.Id, int.MaxValue, token);

            Assert.Equal(1, first.Page.PageNumber);
            Assert.Equal(5, first.Page.PageSize);
            Assert.Equal(7, first.Page.TotalCount);
            Assert.Equal(2, first.Page.TotalPages);
            Assert.Equal(280m, first.TotalAmount);

            Assert.Equal(
                new[] { ids[6], ids[0], ids[1], ids[2], ids[3] },
                first.Page.Items.Select(expense => expense.Id).ToArray());

            Assert.Equal(2, second.Page.PageNumber);
            Assert.Equal(5, second.Page.PageSize);
            Assert.Equal(7, second.Page.TotalCount);
            Assert.Equal(2, second.Page.TotalPages);
            Assert.Equal(280m, second.TotalAmount);

            Assert.Equal(
                new[] { ids[4], ids[5] },
                second.Page.Items.Select(expense => expense.Id).ToArray());

            Assert.Empty(beyondLast.Page.Items);
            Assert.Equal(3, beyondLast.Page.PageNumber);
            Assert.Equal(7, beyondLast.Page.TotalCount);
            Assert.Equal(280m, beyondLast.TotalAmount);

            Assert.Empty(veryLargePage.Page.Items);
            Assert.Equal(int.MaxValue, veryLargePage.Page.PageNumber);
            Assert.Equal(7, veryLargePage.Page.TotalCount);
            Assert.Equal(280m, veryLargePage.TotalAmount);
        }

        [Fact]
        public async Task GetPageAsync_When_Report_Has_No_Expenses_Returns_Empty_Page()
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

            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

            var result = await repository.GetPageAsync(report.Id, 1, token);

            Assert.Empty(result.Page.Items);
            Assert.Equal(1, result.Page.PageNumber);
            Assert.Equal(5, result.Page.PageSize);
            Assert.Equal(0, result.Page.TotalCount);
            Assert.Equal(0, result.Page.TotalPages);
            Assert.Equal(0m, result.TotalAmount);
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
