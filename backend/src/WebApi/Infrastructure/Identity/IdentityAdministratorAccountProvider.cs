using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Npgsql;

using WebApi.Domain.Common;
using WebApi.Application.Common;
using WebApi.Application.Authentication.Errors;
using WebApi.Application.Authentication.Contrats;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Authentication;

namespace WebApi.Infrastructure.Identity
{
    internal sealed class IdentityAdministratorAccountProvider(UserManager<IdentityUser<Guid>> userManager, IUnitOfWork unitOfWork) : IAdministratorAccountProvider
    {
        public async Task<Result<CreatedAdministratorAccount>> CreateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
        {
            try
            {
                return await unitOfWork.ExecuteInTransactionAsync(
                    async token =>
                    {
                        token.ThrowIfCancellationRequested();

                        if (await userManager.FindByEmailAsync(email) is not null)
                            return Result<CreatedAdministratorAccount>.Failure(AdministratorAccountErrors.EmailAlreadyUsed);

                        var account = new IdentityUser<Guid>
                        {
                            Id = Guid.NewGuid(),
                            UserName = email,
                            Email = email
                        };

                        var creation = await userManager.CreateAsync(account, password);

                        if (!creation.Succeeded)
                        {
                            bool duplicate = creation.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");

                            Error error = duplicate ? AdministratorAccountErrors.EmailAlreadyUsed : AdministratorAccountErrors.InvalidAccount;
                            return Result<CreatedAdministratorAccount>.Failure(error);
                        }

                        token.ThrowIfCancellationRequested();

                        var assignment = await userManager.AddToRoleAsync(account, SecurityRoles.Admin);

                        if (!assignment.Succeeded)
                            // Une exception provoque le rollback de la création.
                            throw new InvalidOperationException("Impossible d’affecter le rôle Admin au nouveau compte.");

                        return Result<CreatedAdministratorAccount>.Success(new CreatedAdministratorAccount(account.Id, account.Email!));
                    },
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "UserNameIndex"
                })
            {
                // Couvre également une création concurrente du même compte.
                return Result<CreatedAdministratorAccount>.Failure(AdministratorAccountErrors.EmailAlreadyUsed);
            }
        }
    }
}
