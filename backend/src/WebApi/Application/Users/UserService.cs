using WebApi.Domain.Common;
using WebApi.Domain.Models;

using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;

namespace WebApi.Application.Users
{
    internal sealed class UserService(IUserRepository repository, IUnitOfWork unitOfWork) : IUserService
    {
        public async Task<Result<User>> CreateAsync(UserDetails details, CancellationToken cancellationToken = default)
        {
            var result = User.Create(details.FirstName, details.LastName, details.Street, details.PostalCode, details.City, details.MaxExpensesPerMonth);

            if (!result.IsSuccess)
                return result;

            await repository.AddAsync(result.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}