using FluentValidation;
using WebApi.Infrastructure.Api.Authentication.Contracts;
using WebApi.Infrastructure.Api.Authentication.Errors;

namespace WebApi.Infrastructure.Api.Authentication.Validators
{
    internal sealed class AuthenticationRequestValidator : AbstractValidator<AuthenticationRequest>
    {
        public AuthenticationRequestValidator()
        {
            RuleFor(request => request.Email)
                .NotEmpty()
                .WithErrorCode(AuthenticationRequestErrors.EmailRequired.Code)
                .WithMessage(AuthenticationRequestErrors.EmailRequired.Message);

            RuleFor(request => request.Password)
                .NotEmpty()
                .WithErrorCode(AuthenticationRequestErrors.PasswordRequired.Code)
                .WithMessage(AuthenticationRequestErrors.PasswordRequired.Message);
        }
    }
}
