using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Xunit;
using Testcontainers.PostgreSql;

using WebApi.Infrastructure.Persistence;


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

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(_postgres.GetConnectionString()));
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
    }
}
