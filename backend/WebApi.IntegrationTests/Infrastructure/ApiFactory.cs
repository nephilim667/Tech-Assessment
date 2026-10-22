using System.Net.Http.Json;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Identity.Configuration;
using WebApi.Infrastructure.Api.Authentication.Contracts;

namespace WebApi.IntegrationTests.Infrastructure
{
    public sealed  class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.4").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Les migrations sont pilotées par la fixture.
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remplace la configuration PostgreSQL fournie par Aspire.
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<
                    IDbContextOptionsConfiguration<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options =>  options.UseNpgsql(_postgres.GetConnectionString()));
                services.Configure<BootstrapAdminOptions>(options =>
                {
                    options.Email = "superadmin@example.test";
                    options.Password = "Test-SuperAdmin@2026!";
                });
                services.Configure<JwtOptions>(options =>
                {
                    options.Issuer = "n2jsoft-tests";
                    options.Audience = "n2jsoft-api-tests";
                    options.SigningKey = new string('K', 64);
                    options.ExpirationMinutes = 60;
                });
            });
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                await _postgres.StartAsync();

                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.MigrateAsync();
            }
            catch
            {
                await DisposeAsync();
                throw;
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                await base.DisposeAsync();
            }
            finally
            {
                await _postgres.DisposeAsync();
            }
        }

        public async Task<HttpClient> CreateSuperAdminClientAsync(CancellationToken cancellationToken = default)
        {
            using var scope = Services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedSuperAdminAsync();

            var credentials = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
            var client = CreateClient();

            try
            {
                using var response = await client.PostAsJsonAsync("/api/auth/login",
                    new AuthenticationRequest(credentials.Email, credentials.Password), cancellationToken);

                response.EnsureSuccessStatusCode();

                var authentication = await response.Content
                    .ReadFromJsonAsync<AuthenticationResponse>(cancellationToken)
                    ?? throw new InvalidOperationException(
                        "La réponse d’authentification est vide.");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

                return client;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }
    }
}
