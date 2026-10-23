using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;

using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.ExpenseReports
{
    public sealed class ExpenseReportEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Create_When_Data_Is_Valid_Returns_Created_And_Persists_Report()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<ExpenseReportResponse>(token);

            Assert.NotNull(report);
            Assert.NotEqual(Guid.Empty, report.Id);
            Assert.Equal(userId, report.UserId);
            Assert.Equal("Leblanc Juste - octobre 2026", report.Title);
            Assert.Equal(2026, report.Year);
            Assert.Equal(10, report.Month);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.ExpenseReports
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == report.Id, token);

            Assert.Equal(report.UserId, persisted.UserId);
            Assert.Equal(report.Title, persisted.Title);
            Assert.Equal(report.Year, persisted.Year);
            Assert.Equal(report.Month, persisted.Month);
        }

        [Fact]
        public async Task Create_When_User_Is_Inactive_Returns_Conflict_Without_Persisting()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: false, token);
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await context.ExpenseReports.AnyAsync(report => report.UserId == userId, token));
        }

        [Fact]
        public async Task Create_When_User_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(Guid.NewGuid(), 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(2026, 13)]
        public async Task Create_When_Period_Is_Invalid_Returns_BadRequest(
            int year,
            int month)
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(Guid.NewGuid(), year, month),
                token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(Guid.NewGuid(), 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<Guid> CreateUserAsync(bool isActive, CancellationToken cancellationToken)
        {
            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            if (!isActive)
                user.Deactivate();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
