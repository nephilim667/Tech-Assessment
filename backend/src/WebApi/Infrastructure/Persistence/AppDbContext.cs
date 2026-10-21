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
            new UserEntity { FirstName = "Juste", LastName = "Leblanc" },
            new UserEntity { FirstName = "Marc", LastName = "Assin" }
        );

        await context.SaveChangesAsync(cancellationToken);
    }
}