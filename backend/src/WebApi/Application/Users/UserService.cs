using WebApi.Domain.Common;
using WebApi.Domain.Models;
using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;

namespace WebApi.Application.Users
{
    internal sealed class UserService(IUserRepository repository, IUnitOfWork unitOfWork) : IUserService
    {
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return repository.GetAllAsync(cancellationToken);
        }

        public async Task<Result<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await repository.GetByIdAsync(id, cancellationToken);

            return user is null
                ? Result<User>.Failure(UserErrors.NotFound)
                : Result<User>.Success(user);
        }

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