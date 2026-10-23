using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;

using WebApi.Application.Common;
using WebApi.Application.Interfaces.Authentication;

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
        public async Task Create_When_Period_Is_Invalid_Returns_BadRequest(int year, int month)
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

        [Fact]
        public async Task Create_When_User_Is_Deleted_Returns_NotFound_Without_Persisting()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            // Prépare un bénéficiaire supprimé logiquement.
            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await context.Beneficiaries.SingleAsync(entity => entity.Id == userId, token);

                user.IsDeleted = true;
                user.IsActive = false;

                await context.SaveChangesAsync(token);
            }

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));

            Assert.Equal("user.not_found", problem.RootElement.GetProperty("code").GetString());

            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await verificationContext.ExpenseReports.AnyAsync(report => report.UserId == userId, token));
        }

        [Fact]
        public async Task Create_When_Authenticated_As_Admin_Returns_Created()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            var accessToken = issuer.Generate(Guid.NewGuid(), [SecurityRoles.Admin]);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_Token_Has_No_Allowed_Role_Returns_Forbidden()
        {
            var token = TestContext.Current.CancellationToken;

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            var accessToken = issuer.Generate(Guid.NewGuid(), []);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(Guid.NewGuid(), 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_UserId_Is_Empty_Returns_ValidationError()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(Guid.Empty, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));

            var errors = problem.RootElement.GetProperty("errors");

            Assert.True(errors.TryGetProperty("UserId", out var userErrors));
            Assert.True(userErrors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task Create_Returns_Location_Allowing_Report_To_Be_Read()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var creation = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
            Assert.NotNull(creation.Headers.Location);

            var created = await creation.Content.ReadFromJsonAsync<ExpenseReportResponse>(token);

            Assert.NotNull(created);

            using var response = await client.GetAsync(
                creation.Headers.Location,
                token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<ExpenseReportResponse>(token);

            Assert.Equal(created, report);
        }

        [Fact]
        public async Task GetById_When_Report_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync(
                $"/api/expense-reports/{Guid.NewGuid()}",
                token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.NotFound)]
        public async Task GetById_When_User_Becomes_Inactive_Or_Deleted_Returns_Expected_Status(bool isDeleted,HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var creation = await client.PostAsJsonAsync(
                "/api/expense-reports",
                new CreateExpenseReportRequest(userId, 2026, 10),
                token);

            Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

            var report = await creation.Content.ReadFromJsonAsync<ExpenseReportResponse>(token);

            Assert.NotNull(report);

            // Prépare le nouvel état du bénéficiaire.
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await context.Beneficiaries.SingleAsync(entity => entity.Id == userId, token);

                user.IsActive = false;
                user.IsDeleted = isDeleted;

                await context.SaveChangesAsync(token);
            }

            using var response = await client.GetAsync(
                $"/api/expense-reports/{report.Id}",
                token);

            Assert.Equal(expectedStatus, response.StatusCode);

            // La suppression logique du bénéficiaire conserve bien la note.
            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.True(await verificationContext.ExpenseReports.AnyAsync(entity => entity.Id == report.Id, token));
        }

        [Fact]
        public async Task GetById_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(
                $"/api/expense-reports/{Guid.NewGuid()}",
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
