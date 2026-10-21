using WebApi.Application.Interfaces.Common;

namespace WebApi.Infrastructure.Persistence.Common
{
    internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
