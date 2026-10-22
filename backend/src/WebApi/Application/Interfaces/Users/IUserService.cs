using WebApi.Domain.Common;
using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Users
{
    internal interface IUserService
    {
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<User>> CreateAsync(UserDetails details, CancellationToken cancellationToken = default);
    }
}
