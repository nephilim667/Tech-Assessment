using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Users
{
    internal interface IUserRepository
    {
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
