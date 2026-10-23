using System.Net;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using Npgsql;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Domain.Models;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Infrastructure.Persistence.Entities;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Expenses
{
    public sealed class ExpenseEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Create_When_Data_Is_Valid_Returns_Created_And_Persists_Expense()
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];
            var request = CreateRequest();

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(GetUrl(report.Id), request, token);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>(token);

            Assert.NotNull(expense);
            Assert.NotEqual(Guid.Empty, expense.Id);
            Assert.Equal(report.Id, expense.ExpenseReportId);
            Assert.Equal(request.Date!.Value, expense.Date);
            Assert.Equal(request.Description, expense.Description);
            Assert.Equal(request.Amount, expense.Amount);
            Assert.Equal(request.BillingStreet, expense.BillingStreet);
            Assert.Equal(request.BillingPostalCode, expense.BillingPostalCode);
            Assert.Equal(request.BillingCity, expense.BillingCity);
            Assert.Equal(request.BillingBrand, expense.BillingBrand);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.Equal(expense.ExpenseReportId, persisted.ExpenseReportId);
            Assert.Equal(expense.Amount, persisted.Amount);
            Assert.False(persisted.IsDeleted);
        }

        [Theory]
        [InlineData("inactive", HttpStatusCode.Conflict, "user.inactive")]
        [InlineData("deleted", HttpStatusCode.NotFound, "expense_report.not_found")]
        public async Task Create_When_User_Is_Unavailable_Does_Not_Persist(string state, HttpStatusCode expectedStatus, string expectedCode)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token, state: state))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(GetUrl(report.Id), CreateRequest(), token);

            Assert.Equal(expectedStatus, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));

            Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await context.Expenses.AnyAsync(expense => expense.ExpenseReportId == report.Id, token));
        }

        [Fact]
        public async Task Create_When_Report_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.PostAsJsonAsync(GetUrl(Guid.NewGuid()), CreateRequest(), token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_Date_Is_Missing_Returns_ValidationError()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);
            var request = CreateRequest() with { Date = null };

            using var response = await client.PostAsJsonAsync(GetUrl(Guid.NewGuid()), request, token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Date", out _));
        }

        [Fact]
        public async Task Create_When_Date_Is_Outside_Report_Period_Returns_BadRequest()
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);

            var request = CreateRequest() with
            {
                Date = new DateOnly(2026, 9, 30)
            };

            using var response = await client.PostAsJsonAsync(GetUrl(report.Id), request, token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            Assert.Equal("expense.date_outside_report_period", problem.RootElement.GetProperty("code").GetString());

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await context.Expenses.AnyAsync(expense => expense.ExpenseReportId == report.Id, token));
        }

        [Fact]
        public async Task Create_When_Quota_Is_Reached_In_Another_Report_Returns_Conflict()
        {
            var token = TestContext.Current.CancellationToken;
            var reports = await PrepareReportsAsync(
                token, quota: 1, reportCount: 2);

            using var client = await factory.CreateSuperAdminClientAsync(token);
            using var first = await client.PostAsJsonAsync(GetUrl(reports[0].Id), CreateRequest(), token);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            using var second = await client.PostAsJsonAsync(GetUrl(reports[1].Id), CreateRequest(), token);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync(token));
            Assert.Equal("expense.monthly_quota_reached", problem.RootElement.GetProperty("code").GetString());

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Equal(1,
                await context.Expenses.CountAsync(
                    expense =>
                        expense.ExpenseReportId == reports[0].Id
                        || expense.ExpenseReportId == reports[1].Id,
                    token));
        }

        [Theory]
        [InlineData("Admin", HttpStatusCode.Created)]
        [InlineData("SuperAdmin", HttpStatusCode.Created)]
        [InlineData("", HttpStatusCode.Forbidden)]
        public async Task Create_Applies_Role_Authorization(string role, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            string[] roles = string.IsNullOrEmpty(role) ? [] : [role];
            var accessToken = issuer.Generate(Guid.NewGuid(), roles);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.PostAsJsonAsync(GetUrl(report.Id), CreateRequest(), token);
            Assert.Equal(expectedStatus, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.PostAsJsonAsync(GetUrl(Guid.NewGuid()), CreateRequest(), token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_When_Two_Requests_Compete_For_Last_Slot_Allows_Only_One()
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var token = timeout.Token;

            var reports = await PrepareReportsAsync(token, quota: 1, reportCount: 2);
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var blockingScope = factory.Services.CreateScope();
            var blockingContext = blockingScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var users = blockingScope.ServiceProvider.GetRequiredService<IUserRepository>();
            await using var blockingTransaction = await blockingContext.Database.BeginTransactionAsync(token);

            var user = await users.GetByIdForUpdateAsync(reports[0].UserId, token);
            Assert.NotNull(user);

            // Chaque requête HTTP aura son propre scope, contexte et transaction.
            var requests = new[]
            {
                client.PostAsJsonAsync(GetUrl(reports[0].Id), CreateRequest(), token),
                client.PostAsJsonAsync(GetUrl(reports[1].Id), CreateRequest(), token)
            };

            try
            {
                try
                {
                    await WaitForBlockedUserLocksAsync(
                        blockingContext.Database.GetConnectionString()!,
                        expectedCount: 2,
                        token);
                }
                finally
                {
                    // Libère également le verrou si l’attente échoue.
                    await blockingTransaction.RollbackAsync(CancellationToken.None);
                }

                var responses = await Task.WhenAll(requests);

                Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
                Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

                var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
                using var problem = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync(token));

                Assert.Equal("expense.monthly_quota_reached", problem.RootElement.GetProperty("code").GetString());

                using var verificationScope = factory.Services.CreateScope();
                var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var reportIds = reports.Select(report => report.Id).ToArray();

                Assert.Equal(1,
                    await context.Expenses.CountAsync(
                        expense =>
                            reportIds.Contains(expense.ExpenseReportId)
                            && !expense.IsDeleted,
                        token));
            }
            finally
            {
                // Termine les requêtes et libère leurs réponses, même en cas d’échec.
                await timeout.CancelAsync();

                foreach (var request in requests)
                {
                    try
                    {
                        using var response = await request;
                    }
                    catch (OperationCanceledException)
                    {
                        // Annulation attendue lors du nettoyage.
                    }
                }
            }
        }

        [Fact]
        public async Task GetExpenses_Returns_Pages_Of_Five_And_Global_Total()
        {
            var token = TestContext.Current.CancellationToken;
            var reports = await PrepareReportsAsync(token, reportCount: 2);
            var report = reports[0];

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Six dépenses : total 10 + 20 + ... + 60 = 210.
                context.Expenses.AddRange(
                    Enumerable.Range(1, 6)
                        .Select(day => CreateExpenseEntity(
                            report.Id, day, day * 10m)));

                var deleted = CreateExpenseEntity(report.Id, 31, 1000m);
                deleted.IsDeleted = true;

                context.Expenses.Add(deleted);
                context.Expenses.Add(CreateExpenseEntity(reports[1].Id, 15, 9000m));

                await context.SaveChangesAsync(token);
            }

            using var client = await factory.CreateSuperAdminClientAsync(token);

            // Page 1 par défaut.
            using var firstResponse = await client.GetAsync(GetUrl(report.Id), token);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var first = await firstResponse.Content.ReadFromJsonAsync<ExpensePageResponse>(token);

            Assert.NotNull(first);
            Assert.Equal(1, first.PageNumber);
            Assert.Equal(5, first.PageSize);
            Assert.Equal(6, first.TotalCount);
            Assert.Equal(2, first.TotalPages);
            Assert.Equal(210m, first.TotalAmount);
            Assert.Equal(
                new[] { 6, 5, 4, 3, 2 },
                first.Items.Select(expense => expense.Date.Day).ToArray());

            using var secondResponse = await client.GetAsync($"{GetUrl(report.Id)}?page=2", token);

            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            var second = await secondResponse.Content.ReadFromJsonAsync<ExpensePageResponse>(token);

            Assert.NotNull(second);
            Assert.Equal(2, second.PageNumber);
            Assert.Equal(5, second.PageSize);
            Assert.Equal(6, second.TotalCount);
            Assert.Equal(2, second.TotalPages);
            Assert.Equal(210m, second.TotalAmount);
            Assert.Equal(1, Assert.Single(second.Items).Date.Day);

            using var beyondResponse = await client.GetAsync($"{GetUrl(report.Id)}?page=3", token);

            Assert.Equal(HttpStatusCode.OK, beyondResponse.StatusCode);

            var beyond = await beyondResponse.Content.ReadFromJsonAsync<ExpensePageResponse>(token);

            Assert.NotNull(beyond);
            Assert.Empty(beyond.Items);
            Assert.Equal(3, beyond.PageNumber);
            Assert.Equal(5, beyond.PageSize);
            Assert.Equal(6, beyond.TotalCount);
            Assert.Equal(2, beyond.TotalPages);
            Assert.Equal(210m, beyond.TotalAmount);
        }

        [Fact]
        public async Task GetExpenses_When_Report_Is_Empty_Returns_Empty_Page()
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync(GetUrl(report.Id), token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var page = await response.Content.ReadFromJsonAsync<ExpensePageResponse>(token);

            Assert.NotNull(page);
            Assert.Empty(page.Items);
            Assert.Equal(1, page.PageNumber);
            Assert.Equal(5, page.PageSize);
            Assert.Equal(0, page.TotalCount);
            Assert.Equal(0, page.TotalPages);
            Assert.Equal(0m, page.TotalAmount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetExpenses_When_Page_Is_Invalid_Returns_ValidationError(int page)
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync($"{GetUrl(Guid.NewGuid())}?page={page}", token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));

            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Page", out _));
            Assert.False(problem.RootElement.TryGetProperty("items", out _));
        }

        [Fact]
        public async Task GetExpenses_When_Report_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = await factory.CreateSuperAdminClientAsync(token);

            using var response = await client.GetAsync(GetUrl(Guid.NewGuid()), token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("inactive", HttpStatusCode.OK)]
        [InlineData("deleted", HttpStatusCode.NotFound)]
        public async Task GetExpenses_Applies_User_Visibility(string state, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token, state: state))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);
            using var response = await client.GetAsync(GetUrl(report.Id), token);

            Assert.Equal(expectedStatus, response.StatusCode);
        }

        [Theory]
        [InlineData("Admin", HttpStatusCode.OK)]
        [InlineData("SuperAdmin", HttpStatusCode.OK)]
        [InlineData("", HttpStatusCode.Forbidden)]
        public async Task GetExpenses_Applies_Role_Authorization(string role, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            string[] roles = string.IsNullOrEmpty(role) ? [] : [role];
            var accessToken = issuer.Generate(Guid.NewGuid(), roles);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.GetAsync(GetUrl(report.Id), token);

            Assert.Equal(expectedStatus, response.StatusCode);
        }

        [Fact]
        public async Task GetExpenses_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(GetUrl(Guid.NewGuid()), token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Frees_Quota_Without_Physically_Removing_Expense()
        {
            var token = TestContext.Current.CancellationToken;
            var reports = await PrepareReportsAsync(token, quota: 1, reportCount: 2);

            using var client = await factory.CreateSuperAdminClientAsync(token);
            var expense = await CreateExpenseAsync(client, reports[0].Id, token);

            using var blockedCreation = await client.PostAsJsonAsync(GetUrl(reports[1].Id), CreateRequest(), token);

            Assert.Equal(HttpStatusCode.Conflict, blockedCreation.StatusCode);

            using var deletion = await client.DeleteAsync($"{GetUrl(reports[0].Id)}/{expense.Id}", token);

            Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);

            // Une dépense supprimée n’est plus accessible pour une seconde suppression.
            using var repeatedDeletion = await client.DeleteAsync($"{GetUrl(reports[0].Id)}/{expense.Id}", token);

            Assert.Equal(HttpStatusCode.NotFound, repeatedDeletion.StatusCode);

            using var listing = await client.GetAsync(GetUrl(reports[0].Id), token);
            Assert.Equal(HttpStatusCode.OK, listing.StatusCode);

            var page = await listing.Content.ReadFromJsonAsync<ExpensePageResponse>(token);

            Assert.NotNull(page);
            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalCount);
            Assert.Equal(0m, page.TotalAmount);

            // La place libérée est utilisable dans une autre note du même mois.
            var replacement = await CreateExpenseAsync(client, reports[1].Id, token);

            Assert.NotEqual(expense.Id, replacement.Id);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.True(persisted.IsDeleted);
            Assert.Equal(expense.Amount, persisted.Amount);
        }

        [Theory]
        [InlineData("inactive", HttpStatusCode.NoContent)]
        [InlineData("deleted", HttpStatusCode.NotFound)]
        public async Task Delete_Applies_User_State_Restrictions(string state, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);
            var expense = await CreateExpenseAsync(client, report.Id, token);

            using (var setupScope = factory.Services.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await context.Beneficiaries.SingleAsync(entity => entity.Id == report.UserId, token);

                user.IsActive = false;
                user.IsDeleted = state == "deleted";

                await context.SaveChangesAsync(token);
            }

            using var response = await client.DeleteAsync($"{GetUrl(report.Id)}/{expense.Id}", token);

            Assert.Equal(expectedStatus, response.StatusCode);

            using var verificationScope = factory.Services.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await verificationContext.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.Equal(state == "inactive", persisted.IsDeleted);
        }

        [Fact]
        public async Task Delete_When_Expense_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var client = await factory.CreateSuperAdminClientAsync(token);
            using var response = await client.DeleteAsync($"{GetUrl(report.Id)}/{Guid.NewGuid()}", token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_When_Report_Does_Not_Exist_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;

            using var client = await factory.CreateSuperAdminClientAsync(token);
            using var response = await client.DeleteAsync($"{GetUrl(Guid.NewGuid())}/{Guid.NewGuid()}", token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_When_Expense_Belongs_To_Another_Report_Does_Not_Delete_It()
        {
            var token = TestContext.Current.CancellationToken;
            var reports = await PrepareReportsAsync(token, reportCount: 2);

            using var client = await factory.CreateSuperAdminClientAsync(token);

            var expense = await CreateExpenseAsync(client, reports[0].Id, token);

            using var response = await client.DeleteAsync(
                $"{GetUrl(reports[1].Id)}/{expense.Id}", token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.False(persisted.IsDeleted);
            Assert.Equal(reports[0].Id, persisted.ExpenseReportId);
        }

        [Theory]
        [InlineData("Admin", HttpStatusCode.NoContent)]
        [InlineData("SuperAdmin", HttpStatusCode.NoContent)]
        [InlineData("", HttpStatusCode.Forbidden)]
        public async Task Delete_Applies_Role_Authorization(string role, HttpStatusCode expectedStatus)
        {
            var token = TestContext.Current.CancellationToken;
            var report = (await PrepareReportsAsync(token))[0];

            using var setupClient = await factory.CreateSuperAdminClientAsync(token);
            var expense = await CreateExpenseAsync(setupClient, report.Id, token);

            using var scope = factory.Services.CreateScope();
            var issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();

            string[] roles = string.IsNullOrEmpty(role) ? [] : [role];
            var accessToken = issuer.Generate(Guid.NewGuid(), roles);

            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.DeleteAsync($"{GetUrl(report.Id)}/{expense.Id}", token);

            Assert.Equal(expectedStatus, response.StatusCode);

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Expenses
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == expense.Id, token);

            Assert.Equal(
                expectedStatus == HttpStatusCode.NoContent,
                persisted.IsDeleted);
        }

        [Fact]
        public async Task Delete_When_Anonymous_Returns_Unauthorized()
        {
            var token = TestContext.Current.CancellationToken;
            using var client = factory.CreateClient();

            using var response = await client.DeleteAsync($"{GetUrl(Guid.NewGuid())}/{Guid.NewGuid()}", token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


        // Helpers
        private async Task<ExpenseReport[]> PrepareReportsAsync(CancellationToken token, int quota = 5, string state = "active", int reportCount = 1)
        {
            var user = User.Create(
                "Juste", "Leblanc",
                "10 rue des Lilas", "75001", "Paris", quota).Value;

            var reports = Enumerable.Range(0, reportCount)
                .Select(_ => ExpenseReport.Create(user, 2026, 10).Value)
                .ToArray();

            if (state == "deleted")
                user.Delete();
            else if (state == "inactive")
                user.Deactivate();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Beneficiaries.Add(user.ToEntity());
            await context.SaveChangesAsync(token);

            context.ExpenseReports.AddRange(reports.Select(report => report.ToEntity()));
            await context.SaveChangesAsync(token);

            return reports;
        }

        private static string GetUrl(Guid reportId) => $"/api/expense-reports/{reportId}/expenses";

        private static CreateExpenseRequest CreateRequest() =>
            new(
                Date: new DateOnly(2026, 10, 15),
                Description: "Déjeuner",
                Amount: 25.50m,
                BillingStreet: "20 rue du Restaurant",
                BillingPostalCode: "69001",
                BillingCity: "Lyon");

        private static async Task WaitForBlockedUserLocksAsync(string connectionString, int expectedCount, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT COUNT(*)
                FROM pg_stat_activity
                WHERE datname = current_database()
                    AND pid <> pg_backend_pid()
                    AND wait_event_type = 'Lock'
                    AND query ILIKE '%FROM users%'
                    AND query ILIKE '%FOR UPDATE%'
                """;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(15))
            {
                var count = Convert.ToInt64(
                    await command.ExecuteScalarAsync(cancellationToken));

                if (count >= expectedCount)
                    return;

                await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
            }

            throw new TimeoutException("Les deux requêtes HTTP ne se sont pas retrouvées en attente du verrou utilisateur.");
        }

        private static ExpenseEntity CreateExpenseEntity(Guid reportId, int day, decimal amount) => new()
        {
            Id = Guid.NewGuid(),
            ExpenseReportId = reportId,
            Date = new DateOnly(2026, 10, day),
            Description = "Déjeuner",
            Amount = amount,
            BillingStreet = "20 rue du Restaurant",
            BillingPostalCode = "69001",
            BillingCity = "Lyon",
            IsDeleted = false
        };

        private static async Task<ExpenseResponse> CreateExpenseAsync(HttpClient client, Guid reportId, CancellationToken token)
        {
            using var response = await client.PostAsJsonAsync(GetUrl(reportId), CreateRequest(), token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>(token);
            Assert.NotNull(expense);

            return expense;
        }
    }
}
