using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Infrastructure.Api.Expenses.Contracts;
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

        [Fact]
        public async Task GetReports_Filters_User_And_Period_And_Returns_Multiple_Reports()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);
            var otherUserId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            var first = await CreateReportForListAsync(client, userId, 2026, 10, token);
            var second = await CreateReportForListAsync(client, userId, 2026, 10, token);

            await CreateReportForListAsync(client, userId, 2026, 9, token);
            await CreateReportForListAsync(client, userId, 2025, 10, token);
            await CreateReportForListAsync(client, otherUserId, 2026, 10, token);

            using var response = await client.GetAsync(GetReportsUrl(userId, 2026, 10), token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var reports = await response.Content.ReadFromJsonAsync<ExpenseReportResponse[]>(token);

            Assert.NotNull(reports);

            Assert.Equal(new[] { first.Id, second.Id }.OrderBy(id => id).ToArray(), reports.Select(report => report.Id).ToArray());

            Assert.All(reports, report =>
            {
                Assert.Equal(userId, report.UserId);
                Assert.Equal(2026, report.Year);
                Assert.Equal(10, report.Month);
            });
        }

        [Fact]
        public async Task GetReports_When_Period_Has_No_Report_Returns_Empty_List()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync(GetReportsUrl(userId, 2026, 10), token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var reports = await response.Content.ReadFromJsonAsync<ExpenseReportResponse[]>(token);

            Assert.NotNull(reports);
            Assert.Empty(reports);
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.NotFound)]
        public async Task GetReports_Applies_User_Visibility(bool isDeleted, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);
            var report = await CreateReportForListAsync(client, userId, 2026, 10, token);

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await context.Beneficiaries.SingleAsync(entity => entity.Id == userId, token);

                user.IsActive = false;
                user.IsDeleted = isDeleted;

                await context.SaveChangesAsync(token);
            }

            using var response = await client.GetAsync(GetReportsUrl(userId, 2026, 10), token);

            Assert.Equal(expectedStatus, response.StatusCode);

            if (!isDeleted)
            {
                var reports = await response.Content.ReadFromJsonAsync<ExpenseReportResponse[]>(token);

                Assert.NotNull(reports);
                Assert.Equal(report.Id, Assert.Single(reports).Id);
            }
        }

        [Fact]
        public async Task GetReports_When_User_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync(GetReportsUrl(Guid.NewGuid(), 2026, 10), token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("userId=00000000-0000-0000-0000-000000000000&year=2026&month=10", "UserId")]
        [InlineData("userId=11111111-1111-1111-1111-111111111111&year=0&month=10", "Year")]
        [InlineData("userId=11111111-1111-1111-1111-111111111111&year=2026&month=13", "Month")]
        public async Task GetReports_When_Query_Is_Invalid_Returns_ValidationError(string query, string expectedField)
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync($"/api/expense-reports?{query}", token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));

            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(expectedField, out _));
        }

        [Theory]
        [InlineData("Admin", HttpStatusCode.OK)]
        [InlineData("SuperAdmin", HttpStatusCode.OK)]
        [InlineData("", HttpStatusCode.Forbidden)]
        public async Task GetReports_Applies_Role_Authorization(string role, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            string[] roles = string.IsNullOrEmpty(role) ? [] : [role];
            var accessToken = issuer.Generate(Guid.NewGuid(), roles);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.GetAsync(GetReportsUrl(userId, 2026, 10), token);

            Assert.Equal(expectedStatus, response.StatusCode);
        }

        [Fact]
        public async Task GetReports_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(GetReportsUrl(Guid.NewGuid(), 2026, 10), token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Removes_Report_And_All_Expenses_And_Frees_Quota()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);
            var source = await CreateReportForListAsync(client, userId, 2026, 10, token);
            var remaining = await CreateReportForListAsync(client, userId, 2026, 10, token);

            var expenses = new List<ExpenseResponse>();

            // CreateUserAsync configure un quota de 5.
            for (var index = 0; index < 5; index++)
                expenses.Add(await CreateExpenseForReportAsync(client, source.Id, token));

            using var blockedCreation = await client.PostAsJsonAsync(
                $"/api/expense-reports/{remaining.Id}/expenses",
                CreateExpenseRequestForReport(),
                token);

            Assert.Equal(HttpStatusCode.Conflict, blockedCreation.StatusCode);

            // La cascade doit aussi supprimer cette dépense déjà supprimée logiquement.
            using var softDeletion = await client.DeleteAsync($"/api/expense-reports/{source.Id}/expenses/{expenses[0].Id}", token);
            Assert.Equal(HttpStatusCode.NoContent, softDeletion.StatusCode);

            using var deletion = await client.DeleteAsync($"/api/expense-reports/{source.Id}", token);
            Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);

            using var lookup = await client.GetAsync($"/api/expense-reports/{source.Id}", token);
            Assert.Equal(HttpStatusCode.NotFound, lookup.StatusCode);

            using var repeatedDeletion = await client.DeleteAsync($"/api/expense-reports/{source.Id}", token);
            Assert.Equal(HttpStatusCode.NotFound, repeatedDeletion.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.False(await context.ExpenseReports.AnyAsync(report => report.Id == source.Id, token));
                Assert.False(await context.Expenses.AnyAsync(expense => expense.ExpenseReportId == source.Id, token));
                Assert.True(await context.ExpenseReports.AnyAsync(report => report.Id == remaining.Id, token));
                Assert.True(await context.Beneficiaries.AnyAsync(user => user.Id == userId, token));
            }

            // Une autre note du même mois peut à nouveau recevoir une dépense.
            await CreateExpenseForReportAsync(client, remaining.Id, token);
        }

        [Theory]
        [InlineData(false, HttpStatusCode.NoContent)]
        [InlineData(true, HttpStatusCode.NotFound)]
        public async Task Delete_Applies_User_State_Restrictions(bool isDeleted, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            var report = await CreateReportForListAsync(client, userId, 2026, 10, token);
            var expense = await CreateExpenseForReportAsync(client, report.Id, token);

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await context.Beneficiaries.SingleAsync(entity => entity.Id == userId, token);

                user.IsActive = false;
                user.IsDeleted = isDeleted;

                await context.SaveChangesAsync(token);
            }

            using var response = await client.DeleteAsync($"/api/expense-reports/{report.Id}", token);
            Assert.Equal(expectedStatus, response.StatusCode);

            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Equal(isDeleted, await verificationContext.ExpenseReports.AnyAsync(entity => entity.Id == report.Id, token));
            Assert.Equal(isDeleted, await verificationContext.Expenses.AnyAsync(entity => entity.Id == expense.Id, token));
        }

        [Fact]
        public async Task Delete_When_Report_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.DeleteAsync($"/api/expense-reports/{Guid.NewGuid()}", token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            Assert.Equal("expense_report.not_found", problem.RootElement.GetProperty("code").GetString());
        }

        [Theory]
        [InlineData("Admin", HttpStatusCode.NoContent)]
        [InlineData("SuperAdmin", HttpStatusCode.NoContent)]
        [InlineData("", HttpStatusCode.Forbidden)]
        public async Task Delete_Applies_Role_Authorization(string role, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var userId = await CreateUserAsync(isActive: true, token);

            using var setupClient = await factory.CreateSuperAdminClientAsync(token);

            var report = await CreateReportForListAsync(setupClient, userId, 2026, 10, token);

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            string[] roles = string.IsNullOrEmpty(role) ? [] : [role];
            var accessToken = issuer.Generate(Guid.NewGuid(), roles);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.DeleteAsync($"/api/expense-reports/{report.Id}", token);
            Assert.Equal(expectedStatus, response.StatusCode);

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(expectedStatus == HttpStatusCode.Forbidden, await context.ExpenseReports.AnyAsync(entity => entity.Id == report.Id, token));
        }

        [Fact]
        public async Task Delete_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.DeleteAsync($"/api/expense-reports/{Guid.NewGuid()}", token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


        // Helpers
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
        
        private static string GetReportsUrl(Guid userId, int year, int month) => $"/api/expense-reports?userId={userId}&year={year}&month={month}";

        private static async Task<ExpenseReportResponse> CreateReportForListAsync(HttpClient client, Guid userId, int year, int month, CancellationToken token)
        {
            using var response = await client.PostAsJsonAsync("/api/expense-reports", new CreateExpenseReportRequest(userId, year, month), token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<ExpenseReportResponse>(token);
            Assert.NotNull(report);

            return report;
        }

        private static CreateExpenseRequest CreateExpenseRequestForReport() => new(
            Date: new DateOnly(2026, 10, 15),
            Description: "Déjeuner",
            Amount: 25.50m,
            BillingStreet: "20 rue du Restaurant",
            BillingPostalCode: "69001",
            BillingCity: "Lyon");

        private static async Task<ExpenseResponse> CreateExpenseForReportAsync(HttpClient client, Guid reportId, CancellationToken token)
        {
            using var response = await client.PostAsJsonAsync($"/api/expense-reports/{reportId}/expenses", CreateExpenseRequestForReport(), token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>(token);
            Assert.NotNull(expense);

            return expense;
        }
    }
}
