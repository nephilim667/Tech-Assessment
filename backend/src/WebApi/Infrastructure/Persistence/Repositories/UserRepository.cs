using WebApi.Application.Interfaces.Users;
using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence.Mappings;


namespace WebApi.Infrastructure.Persistence.Repositories
{
    internal sealed class UserRepository(AppDbContext context): IUserRepository
    {
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await context.Users.AddAsync(user.ToEntity(), cancellationToken);
        }
    }
}
