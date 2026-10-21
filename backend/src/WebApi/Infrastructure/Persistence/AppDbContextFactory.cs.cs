using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebApi.Infrastructure.Persistence
{
    internal sealed class AppDbContextFactory :IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // ConnectionStrings__database est la représentation en variable d’environnement de ConnectionStrings:database : les deux underscores séparent les niveaux de configuration .NET.
            // Configuration propre au projet avec utilisation de Aspire
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__database");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "La variable ConnectionStrings__database doit être définie pour utiliser les outils de migration EF Core.");
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}
