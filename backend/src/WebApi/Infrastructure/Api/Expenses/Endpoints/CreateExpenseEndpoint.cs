using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.Expenses;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Mappings;

namespace WebApi.Infrastructure.Api.Expenses.Endpoints
{
    internal static class CreateExpenseEndpoint
    {
        public static async Task<IResult> CreateAsync(Guid expenseReportId, CreateExpenseRequest request, IExpenseService service, CancellationToken cancellationToken)
        {
            var result = await service.CreateAsync(expenseReportId, request.ToDetails(), cancellationToken);

            if (result.IsSuccess)
                return Results.Json(result.Value.ToResponse(), statusCode: StatusCodes.Status201Created);

            var error = result.Error;
            var statusCode = StatusCodes.Status400BadRequest;

            if (error == UserErrors.NotFound || error == ExpenseReportErrors.NotFound)
                statusCode = StatusCodes.Status404NotFound;
            else if (error == UserErrors.Inactive || error == UserErrors.Deleted || error == ExpenseErrors.MonthlyQuotaReached)
                statusCode = StatusCodes.Status409Conflict;

            return Results.Problem(statusCode: statusCode, detail: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                });
        }
    }
}
