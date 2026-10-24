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
    public sealed class ExpenseReportPersistenceTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task SaveChangesAsync_Allows_Several_Reports_For_Same_User_And_Month()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = CreateUser();
            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(token);

            var first = ExpenseReport.Create(user, 2026, 10).Value;
            var second = ExpenseReport.Create(user, 2026, 10).Value;

            context.ExpenseReports.AddRange(first.ToEntity(), second.ToEntity());

            // Act
            await context.SaveChangesAsync(token);

            // Assert
            var reports = await context.ExpenseReports
                .AsNoTracking()
                .Where(report =>
                    report.UserId == user.Id
                    && report.Year == 2026
                    && report.Month == 10)
                .ToListAsync(token);

            Assert.Equal(2, reports.Count);
            Assert.Contains(reports, report => report.Id == first.Id);
            Assert.Contains(reports, report => report.Id == second.Id);
        }

        [Fact]
        public async Task SaveChangesAsync_When_User_Does_Not_Exist_Rejects_Report()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.ExpenseReports.Add(CreateReportEntity(Guid.NewGuid()));

            // Act
            var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(token));

            // Assert
            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
        }

        [Theory]
        [InlineData(0, 10, "CK_expense_reports_Year")]
        [InlineData(2026, 13, "CK_expense_reports_Month")]
        public async Task SaveChangesAsync_When_Period_Is_Invalid_Rejects_Report(int year, int month, string expectedConstraint)
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = CreateUser();
            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(token);

            var entity = CreateReportEntity(user.Id);
            entity.Year = year;
            entity.Month = month;

            context.ExpenseReports.Add(entity);

            // Act
            var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(token));

            // Assert
            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(PostgresErrorCodes.CheckViolation, postgresException.SqlState);
            Assert.Equal(expectedConstraint, postgresException.ConstraintName);
        }

        [Fact]
        public async Task SaveChangesAsync_Generates_Unique_Numbers_Across_Users()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var firstUser = CreateUser();
            var secondUser = CreateUser();

            context.Beneficiaries.AddRange(firstUser.ToEntity(), secondUser.ToEntity());
            await context.SaveChangesAsync(token);

            var firstReport = ExpenseReport.Create(firstUser, 2026, 10).Value.ToEntity();
            var secondReport = ExpenseReport.Create(secondUser, 2026, 10).Value.ToEntity();

            context.ExpenseReports.AddRange(firstReport, secondReport);

            await context.SaveChangesAsync(token);

            Assert.True(firstReport.Number > 0);
            Assert.True(secondReport.Number > 0);
            Assert.NotEqual(firstReport.Number, secondReport.Number);

            context.ChangeTracker.Clear();

            var persisted = await context.ExpenseReports
                .AsNoTracking()
                .SingleAsync(report => report.Id == firstReport.Id, token);

            Assert.Equal(firstReport.Number, persisted.Number);
        }

        [Fact]
        public async Task SaveChangesAsync_Does_Not_Reuse_A_Deleted_Report_Number()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = CreateUser();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(token);

            var firstReport = ExpenseReport.Create(user, 2026, 10).Value.ToEntity();

            context.ExpenseReports.Add(firstReport);
            await context.SaveChangesAsync(token);

            var deletedNumber = firstReport.Number;

            context.ExpenseReports.Remove(firstReport);
            await context.SaveChangesAsync(token);

            var secondReport = ExpenseReport.Create(user, 2026, 10).Value.ToEntity();

            context.ExpenseReports.Add(secondReport);
            await context.SaveChangesAsync(token);

            Assert.True(secondReport.Number > deletedNumber);
        }

        private static User CreateUser() =>
            User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

        private static ExpenseReportEntity CreateReportEntity(Guid userId) =>
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Leblanc Juste - octobre 2026",
                Year = 2026,
                Month = 10
            };
    }
}
