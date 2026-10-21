using FluentValidation;
using WebApi.Domain.Errors;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.Infrastructure.Api.Users.Validators
{
    internal sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(request => request.MaxExpensesPerMonth)
                .GreaterThanOrEqualTo(1)
                .WithErrorCode(UserErrors.InvalidMonthlyQuota.Code)
                .WithMessage(UserErrors.InvalidMonthlyQuota.Message);

            RuleFor(request => request.FirstName)
                .NotEmpty()
                .WithErrorCode(UserErrors.InvalidFirstName.Code)
                .WithMessage(UserErrors.InvalidFirstName.Message);

            RuleFor(request => request.LastName)
                .NotEmpty()
                .WithErrorCode(UserErrors.InvalidLastName.Code)
                .WithMessage(UserErrors.InvalidLastName.Message);

            RuleFor(request => request.Street)
                .NotEmpty()
                .WithErrorCode(UserErrors.InvalidStreet.Code)
                .WithMessage(UserErrors.InvalidStreet.Message);

            RuleFor(request => request.PostalCode)
                .NotEmpty()
                .WithErrorCode(UserErrors.InvalidPostalCode.Code)
                .WithMessage(UserErrors.InvalidPostalCode.Message);

            RuleFor(request => request.City)
                .NotEmpty()
                .WithErrorCode(UserErrors.InvalidCity.Code)
                .WithMessage(UserErrors.InvalidCity.Message);
        }
    }
}
