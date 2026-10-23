using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Users;
using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Users
{
    internal interface IUserService
    {
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<User>> CreateAsync(UserDetails details, CancellationToken cancellationToken = default);
        Task<Result<User>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
        Task<Result<User>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
