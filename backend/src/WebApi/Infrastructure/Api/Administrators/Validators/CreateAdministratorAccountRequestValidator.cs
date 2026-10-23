using FluentValidation;
using WebApi.Infrastructure.Api.Administrators.Errors;
using WebApi.Infrastructure.Api.Administrators.Contracts;

namespace WebApi.Infrastructure.Api.Administrators.Validators
{
    internal sealed class CreateAdministratorAccountRequestValidator : AbstractValidator<CreateAdministratorAccountRequest>
    {
        public CreateAdministratorAccountRequestValidator()
        {
            RuleFor(request => request.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(AdministratorAccountRequestErrors.InvalidEmail.Code)
                .WithMessage(AdministratorAccountRequestErrors.InvalidEmail.Message)
                .EmailAddress()
                .WithErrorCode(AdministratorAccountRequestErrors.InvalidEmail.Code)
                .WithMessage(AdministratorAccountRequestErrors.InvalidEmail.Message);

            RuleFor(request => request.Password)
                .Must(IsStrongPassword)
                .WithErrorCode(AdministratorAccountRequestErrors.InvalidPassword.Code)
                .WithMessage(AdministratorAccountRequestErrors.InvalidPassword.Message);
        }

        private static bool IsStrongPassword(string? password)
        {
            return password is { Length: >= 12 }
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(character => !char.IsLetterOrDigit(character));
        }
    }
}
