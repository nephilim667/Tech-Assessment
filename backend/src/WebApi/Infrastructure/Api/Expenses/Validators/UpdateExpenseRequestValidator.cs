using FluentValidation;

using WebApi.Infrastructure.Api.Expenses.Errors;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Interfaces;

namespace WebApi.Infrastructure.Api.Expenses.Validators
{
    internal sealed class UpdateExpenseRequestValidator : ExpenseDetailsRequestValidator<UpdateExpenseRequest>
    {
        public UpdateExpenseRequestValidator()
        {
            RuleFor(request => request.DestinationExpenseReportId)
                .NotEmpty()
                .WithMessage(ExpenseRequestErrors.DestinationRequired.Message)
                .WithErrorCode(ExpenseRequestErrors.DestinationRequired.Code);
        }
    }
}
