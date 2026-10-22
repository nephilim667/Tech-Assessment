using Microsoft.AspNetCore.Identity;
using WebApi.Application.Authentication;
using WebApi.Application.Authentication.Errors;
using WebApi.Application.Authentication.Tokens;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Identity
{
    internal sealed class IdentityAuthenticationProvider(UserManager<IdentityUser<Guid>> userManager, SignInManager<IdentityUser<Guid>> signInManager, IAccessTokenIssuer tokenIssuer) : IAuthenticationProvider
    {
        public async Task<Result<IssuedAccessToken>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var account = await userManager.FindByEmailAsync(email);

            if (account is null)
                return Result<IssuedAccessToken>.Failure(AuthenticationErrors.InvalidCredentials);

            var signInResult = await signInManager.CheckPasswordSignInAsync(account, password, lockoutOnFailure: true);

            if (!signInResult.Succeeded)
                return Result<IssuedAccessToken>.Failure(AuthenticationErrors.InvalidCredentials);

            var roles = await userManager.GetRolesAsync(account);

            cancellationToken.ThrowIfCancellationRequested();

            return Result<IssuedAccessToken>.Success(tokenIssuer.Generate(account.Id, roles.ToArray()));
        }
    }
}
