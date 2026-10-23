using FluentValidation;

using WebApi.Domain.Errors;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Errors;

namespace WebApi.Infrastructure.Api.ExpenseReports.Validators
{
    internal sealed class CreateExpenseReportRequestValidator : AbstractValidator<CreateExpenseReportRequest>
    {
        public CreateExpenseReportRequestValidator()
        {
            RuleFor(request => request.UserId)
                .NotEmpty()
                .WithMessage(ExpenseReportRequestErrors.UserRequired.Message)
                .WithErrorCode(ExpenseReportRequestErrors.UserRequired.Code);

            RuleFor(request => request.Year)
                .InclusiveBetween(1, 9999)
                .WithMessage(ExpenseReportErrors.InvalidPeriod.Message)
                .WithErrorCode(ExpenseReportErrors.InvalidPeriod.Code);

            RuleFor(request => request.Month)
                .InclusiveBetween(1, 12)
                .WithMessage(ExpenseReportErrors.InvalidPeriod.Message)
                .WithErrorCode(ExpenseReportErrors.InvalidPeriod.Code);
        }
    }
}
