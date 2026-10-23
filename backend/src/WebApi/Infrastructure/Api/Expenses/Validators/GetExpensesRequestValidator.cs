using FluentValidation;

using WebApi.Application.Common.Errors;
using WebApi.Infrastructure.Api.Expenses.Contracts;

namespace WebApi.Infrastructure.Api.Expenses.Validators
{
    internal sealed class GetExpensesRequestValidator : AbstractValidator<GetExpensesRequest>
    {
        public GetExpensesRequestValidator()
        {
            RuleFor(request => request.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage(PaginationErrors.InvalidPage.Message)
                .WithErrorCode(PaginationErrors.InvalidPage.Code);
        }
    }
}
