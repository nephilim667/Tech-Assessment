using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Users
{
    internal interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
