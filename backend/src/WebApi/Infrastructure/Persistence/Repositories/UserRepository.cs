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
            var entities = await context.Users
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
            var entity = await context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == id && !user.IsDeleted,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await context.Users.AddAsync(user.ToEntity(), cancellationToken);
        }
    }
}
