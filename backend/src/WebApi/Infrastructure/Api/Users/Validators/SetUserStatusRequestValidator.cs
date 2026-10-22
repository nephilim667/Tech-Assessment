using FluentValidation;

using WebApi.Infrastructure.Api.Users.Errors;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.Infrastructure.Api.Users.Validators
{
    internal sealed class SetUserStatusRequestValidator : AbstractValidator<SetUserStatusRequest>
    {
        public SetUserStatusRequestValidator()
        {
            RuleFor(request => request.IsActive)
                .NotNull()
                .WithErrorCode(UserRequestErrors.StatusRequired.Code)
                .WithMessage(UserRequestErrors.StatusRequired.Message);
        }
    }
}
