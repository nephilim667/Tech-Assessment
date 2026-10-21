using Microsoft.EntityFrameworkCore;

using WebApi.Infrastructure.Persistence.Entities;

namespace WebApi.Infrastructure.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    internal static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<UserEntity>().AnyAsync(cancellationToken))
            return;

        context.Set<UserEntity>().AddRange(
            new UserEntity { FirstName = "Juste", LastName = "Leblanc", Street = "10 rue des Lilas", PostalCode = "75001", City = "Paris", MaxExpensesPerMonth = 5, IsActive = true },
            new UserEntity { FirstName = "Marc", LastName = "Assin", Street = "20 rue des Roses", PostalCode = "69001", City = "Lyon", MaxExpensesPerMonth = 10, IsActive = true }
        );

        await context.SaveChangesAsync(cancellationToken);
    }
}