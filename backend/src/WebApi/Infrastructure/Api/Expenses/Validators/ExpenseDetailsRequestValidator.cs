using FluentValidation;

using WebApi.Domain.Errors;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Errors;
using WebApi.Infrastructure.Api.Expenses.Interfaces;

namespace WebApi.Infrastructure.Api.Expenses.Validators
{
    internal abstract class ExpenseDetailsRequestValidator<TRequest> : AbstractValidator<TRequest> where TRequest : IExpenseDetailsRequest
    {
        protected ExpenseDetailsRequestValidator()
        {
            RuleFor(request => request.Date)
                .NotNull()
                .WithMessage(ExpenseRequestErrors.DateRequired.Message)
                .WithErrorCode(ExpenseRequestErrors.DateRequired.Code);

            RuleFor(request => request.Description)
                .Must(description =>
                    !string.IsNullOrWhiteSpace(description)
                    && description.Length <= 50)
                .WithMessage(ExpenseErrors.InvalidDescription.Message)
                .WithErrorCode(ExpenseErrors.InvalidDescription.Code);

            RuleFor(request => request.Amount)
                .Must(amount =>
                    amount > 0
                    && decimal.Round(amount, 2) == amount)
                .WithMessage(ExpenseErrors.InvalidAmount.Message)
                .WithErrorCode(ExpenseErrors.InvalidAmount.Code);

            RuleFor(request => request.BillingStreet)
                .NotEmpty()
                .WithMessage(ExpenseErrors.BillingStreetRequired.Message)
                .WithErrorCode(ExpenseErrors.BillingStreetRequired.Code);

            RuleFor(request => request.BillingPostalCode)
                .NotEmpty()
                .WithMessage(ExpenseErrors.BillingPostalCodeRequired.Message)
                .WithErrorCode(ExpenseErrors.BillingPostalCodeRequired.Code);

            RuleFor(request => request.BillingCity)
                .NotEmpty()
                .WithMessage(ExpenseErrors.BillingCityRequired.Message)
                .WithErrorCode(ExpenseErrors.BillingCityRequired.Code);
        }
    }
}
