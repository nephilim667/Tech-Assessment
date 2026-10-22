using Microsoft.EntityFrameworkCore;
using WebApi.Application.Interfaces.Common;

namespace WebApi.Infrastructure.Persistence.Common
{
    internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // En cas d’exception, le await using libère la transaction non validée et provoque son rollback.
        // Le contexte de cette opération doit ensuite être abandonné : le rollback ne réinitialise pas son tracking
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation,CancellationToken cancellationToken = default)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
    }
}