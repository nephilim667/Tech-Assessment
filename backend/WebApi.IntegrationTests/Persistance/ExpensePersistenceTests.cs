using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Entities;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Persistance
{
    public sealed class ExpensePersistenceTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task SaveChangesAsync_Preserves_Expense_Data()
        {
            var token = TestContext.Current.CancellationToken;
            var reportId = await CreateReportAsync(token);
            var entity = CreateExpenseEntity(reportId);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Expenses.Add(entity);
                await context.SaveChangesAsync(token);
            }

            // Lecture dans un autre contexte pour vérifier la persistance réelle.
            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await verificationContext.Expenses.AsNoTracking().SingleAsync(expense => expense.Id == entity.Id, token);

            Assert.Equal(entity.ExpenseReportId, persisted.ExpenseReportId);
            Assert.Equal(entity.Date, persisted.Date);
            Assert.Equal(entity.Description, persisted.Description);
            Assert.Equal(entity.Amount, persisted.Amount);
            Assert.Equal(entity.BillingStreet, persisted.BillingStreet);
            Assert.Equal(entity.BillingPostalCode, persisted.BillingPostalCode);
            Assert.Equal(entity.BillingCity, persisted.BillingCity);
            Assert.Null(persisted.BillingBrand);
            Assert.False(persisted.IsDeleted);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("12.345")]
        public async Task SaveChangesAsync_When_Amount_Is_Invalid_Rejects_Expense(
            string amount)
        {
            var token = TestContext.Current.CancellationToken;
            var reportId = await CreateReportAsync(token);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var entity = CreateExpenseEntity(reportId);
            entity.Amount = decimal.Parse(amount, CultureInfo.InvariantCulture);

            context.Expenses.Add(entity);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(token));
            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(PostgresErrorCodes.CheckViolation, postgresException.SqlState);
            Assert.Equal("CK_expenses_Amount", postgresException.ConstraintName);
        }

        [Fact]
        public async Task SaveChangesAsync_When_Report_Does_Not_Exist_Rejects_Expense()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Expenses.Add(CreateExpenseEntity(Guid.NewGuid()));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(token));

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
            Assert.Equal("FK_expenses_expense_reports_ExpenseReportId", postgresException.ConstraintName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Delete_Report_Physically_Deletes_Its_Expenses(
            bool isDeleted)
        {
            var token = TestContext.Current.CancellationToken;
            var reportId = await CreateReportAsync(token);

            var expense = CreateExpenseEntity(reportId);
            expense.IsDeleted = isDeleted;

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Expenses.Add(expense);
                await context.SaveChangesAsync(token);
            }

            // Aucun chargement des dépenses : la cascade doit être faite par PostgreSQL.
            using (var deletionScope = factory.Services.CreateScope())
            {
                var context = deletionScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var report = await context.ExpenseReports
                    .SingleAsync(entity => entity.Id == reportId, token);

                context.ExpenseReports.Remove(report);
                await context.SaveChangesAsync(token);
            }

            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await verificationContext.ExpenseReports.AnyAsync(report => report.Id == reportId, token));
            Assert.False(await verificationContext.Expenses.AnyAsync(entity => entity.ExpenseReportId == reportId, token));
        }

        private async Task<Guid> CreateReportAsync(CancellationToken cancellationToken)
        {
            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            var report = ExpenseReport.Create(user, 2026, 10).Value;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(cancellationToken);

            context.ExpenseReports.Add(report.ToEntity());
            await context.SaveChangesAsync(cancellationToken);

            return report.Id;
        }

        private static ExpenseEntity CreateExpenseEntity(Guid reportId) =>
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseReportId = reportId,
                Date = new DateOnly(2026, 10, 15),
                Description = "Déjeuner",
                Amount = 25.50m,
                BillingStreet = "20 rue du Restaurant",
                BillingPostalCode = "69001",
                BillingCity = "Lyon",
                BillingBrand = null,
                IsDeleted = false
            };
    }
}
