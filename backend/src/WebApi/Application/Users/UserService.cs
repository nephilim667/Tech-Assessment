using WebApi.Domain.Common;
using WebApi.Domain.Models;
using WebApi.Domain.Errors;
using WebApi.Domain.Contracts.Users;

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

        public Task<Result<User>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var user = await repository.GetByIdForUpdateAsync(id, token);

                    if (user is null)
                        return Result<User>.Failure(UserErrors.NotFound);

                    var result = isActive ? user.Activate() : user.Deactivate();

                    if (!result.IsSuccess)
                        return result;

                    await repository.UpdateStateAsync(user, token);
                    await unitOfWork.SaveChangesAsync(token);

                    return result;
                },
                cancellationToken);
        }

        public Task<Result<User>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var user = await repository.GetByIdForUpdateAsync(id, token);

                    if (user is null)
                        return Result<User>.Failure(UserErrors.NotFound);

                    var result = user.Delete();

                    if (!result.IsSuccess)
                        return result;

                    await repository.UpdateStateAsync(user, token);
                    await unitOfWork.SaveChangesAsync(token);

                    return result;
                },
                cancellationToken);
        }
    }
}