using WebApi.Domain.Common;
using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Users
{
    internal interface IUserService
    {
        Task<Result<User>> CreateAsync(UserDetails details, CancellationToken cancellationToken = default);
    }
}
