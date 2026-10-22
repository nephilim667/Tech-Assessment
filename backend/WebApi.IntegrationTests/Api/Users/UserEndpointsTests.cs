using System.Net;
using System.Text.Json;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Persistence.Entities;

using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Api.Users
{
    public sealed class UserEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory = factory;

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

            var persisted = await context.Users.AsNoTracking().SingleAsync(user => user.Id == body.Id, cancellationToken);

            Assert.Equal(request.Street, persisted.Street);
            Assert.Equal(request.PostalCode, persisted.PostalCode);
            Assert.Equal(request.City, persisted.City);
            Assert.Equal(request.MaxExpensesPerMonth, persisted.MaxExpensesPerMonth);

            Assert.Equal($"/api/users/{body.Id}", response.Headers.Location?.OriginalString);

            using var getResponse = await client.GetAsync(response.Headers.Location, cancellationToken);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrieved = await getResponse.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);

            Assert.NotNull(retrieved);
            Assert.Equal(body, retrieved);
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

        [Fact]
        public async Task GetUser_When_User_Does_Not_Exist_Returns_NotFound()
        {
            using var client = _factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            using var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}", cancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            Assert.Equal("user.not_found", body.RootElement.GetProperty("code").GetString());
        }

        [Fact]
        public async Task GetUser_When_User_Is_Deleted_Returns_NotFound()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            var request = new CreateUserRequest(
                "Paul",
                "Durand",
                "8 rue des Écoles",
                "44000",
                "Nantes",
                5);

            using var createResponse = await client.PostAsJsonAsync("/api/users", request, cancellationToken);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);

            Assert.NotNull(created);

            // Préparation de l’état persisté :
            // le parcours HTTP de suppression n’existe pas encore.
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var entity = await context.Users.SingleAsync(
                    user => user.Id == created.Id,
                    cancellationToken);

                entity.IsDeleted = true;
                entity.IsActive = false;

                await context.SaveChangesAsync(cancellationToken);
            }

            // Act
            using var response = await client.GetAsync($"/api/users/{created.Id}", cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            Assert.Equal("user.not_found", body.RootElement.GetProperty("code").GetString());
        }

        [Fact]
        public async Task GetUsers_Returns_Active_And_Inactive_Users_Without_Deleted()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            var active = CreateEntity("Actif", true, false);
            var inactive = CreateEntity("Inactif", false, false);
            var deleted = CreateEntity("Supprimé", false, true);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Users.AddRange(active, inactive, deleted);
                await context.SaveChangesAsync(cancellationToken);
            }

            // Act
            using var response = await client.GetAsync("/api/users", cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var users = await response.Content.ReadFromJsonAsync<UserResponse[]>(cancellationToken);

            Assert.NotNull(users);
            Assert.Contains(users, user => user.Id == active.Id && user.IsActive);
            Assert.Contains(users, user => user.Id == inactive.Id && !user.IsActive);
            Assert.DoesNotContain(users, user => user.Id == deleted.Id);


            // Helper
            static UserEntity CreateEntity(string firstName, bool isActive, bool isDeleted)
            {
                return new UserEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = "Test",
                    Street = "10 rue des Lilas",
                    PostalCode = "75001",
                    City = "Paris",
                    MaxExpensesPerMonth = 5,
                    IsActive = isActive,
                    IsDeleted = isDeleted
                };
            }
        }
    }
}
