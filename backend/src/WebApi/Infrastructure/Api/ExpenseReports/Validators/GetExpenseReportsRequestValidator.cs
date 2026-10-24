using FluentValidation;

using WebApi.Domain.Errors;
using WebApi.Infrastructure.Api.ExpenseReports.Errors;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;

namespace WebApi.Infrastructure.Api.ExpenseReports.Validators
{
    internal sealed class GetExpenseReportsRequestValidator : AbstractValidator<GetExpenseReportsRequest>
    {
        public GetExpenseReportsRequestValidator()
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
