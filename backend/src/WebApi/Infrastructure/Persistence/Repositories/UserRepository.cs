using Microsoft.EntityFrameworkCore;

using WebApi.Application.Interfaces.Users;
using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence.Mappings;


namespace WebApi.Infrastructure.Persistence.Repositories
{
    internal sealed class UserRepository(AppDbContext context): IUserRepository
    {
        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await context.Beneficiaries
                .AsNoTracking()
                .Where(user => !user.IsDeleted)
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ThenBy(user => user.Id)
                .ToListAsync(cancellationToken);

            return entities.Select(entity => entity.ToDomain()).ToArray();
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await context.Beneficiaries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == id && !user.IsDeleted,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await context.Beneficiaries.AddAsync(user.ToEntity(), cancellationToken);
        }

        public async Task UpdateStateAsync(User user, CancellationToken cancellationToken = default)
        {
            var entity = await context.Beneficiaries.SingleAsync(
                entity => entity.Id == user.Id && !entity.IsDeleted,
                cancellationToken);

            user.ApplyStateTo(entity);
        }

        public async Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (context.Database.CurrentTransaction is null)
                throw new InvalidOperationException("La lecture verrouillée nécessite une transaction active.");

            var entity = await context.Beneficiaries
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM users
                    WHERE "Id" = {id}
                    AND NOT "IsDeleted"
                    FOR UPDATE
                 """)
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }
    }
}
