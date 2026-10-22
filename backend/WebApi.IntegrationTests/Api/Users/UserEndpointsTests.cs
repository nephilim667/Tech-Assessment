using System.Net;
using System.Text.Json;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using WebApi.Infrastructure.Persistence;
using WebApi.IntegrationTests.Infrastructure;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.IntegrationTests.Api.Users
{
    public sealed class UserEndpointsTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public UserEndpointsTests(ApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_Returns_Ok()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            using var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_When_Request_Is_Valid_Returns_Created_And_Persists()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            var request = new CreateUserRequest(
                "Alice",
                "Martin",
                "12 rue des Fleurs",
                "33000",
                "Bordeaux",
                5);

            // Act
            using var response = await client.PostAsJsonAsync("/api/users", request, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);

            Assert.NotNull(body);
            Assert.NotEqual(Guid.Empty, body.Id);
            Assert.Equal(request.FirstName, body.FirstName);
            Assert.Equal(request.LastName, body.LastName);
            Assert.Equal(request.MaxExpensesPerMonth, body.MaxExpensesPerMonth);
            Assert.True(body.IsActive);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await context.Users
                .AsNoTracking()
                .SingleAsync(user => user.Id == body.Id, cancellationToken);

            Assert.Equal(request.Street, persisted.Street);
            Assert.Equal(request.PostalCode, persisted.PostalCode);
            Assert.Equal(request.City, persisted.City);
            Assert.Equal(request.MaxExpensesPerMonth, persisted.MaxExpensesPerMonth);
        }

        [Fact]
        public async Task CreateUser_When_Request_Is_Invalid_Returns_Errors_Without_Inserting()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var initialCount = await context.Users.CountAsync(cancellationToken);

            var request = new CreateUserRequest(
                "",
                "Martin",
                "12 rue des Fleurs",
                "33000",
                "Bordeaux",
                0);

            // Act
            using var response = await client.PostAsJsonAsync("/api/users", request, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            var errors = body.RootElement.GetProperty("errors");

            Assert.True(errors.GetProperty("FirstName").GetArrayLength() > 0);
            Assert.True(errors.GetProperty("MaxExpensesPerMonth").GetArrayLength() > 0);

            Assert.Equal(initialCount, await context.Users.CountAsync(cancellationToken));
        }
    }
}
