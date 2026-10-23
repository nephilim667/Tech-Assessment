using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.Expenses;

using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Mappings;

namespace WebApi.Infrastructure.Api.Expenses.Endpoints
{
    internal static class UpdateExpenseEndpoint
    {
        public static async Task<IResult> UpdateAsync(Guid expenseReportId, Guid expenseId, UpdateExpenseRequest request, IExpenseService service, CancellationToken cancellationToken)
        {
            var result = await service.UpdateAsync(
                expenseReportId,
                expenseId,
                request.DestinationExpenseReportId,
                request.ToDetails(),
                cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value.ToResponse());

            var error = result.Error;
            var statusCode = StatusCodes.Status400BadRequest;

            if (error == ExpenseReportErrors.NotFound || error == ExpenseErrors.NotFound || error == UserErrors.NotFound)
                statusCode = StatusCodes.Status404NotFound;
            else if (error == UserErrors.Inactive || error == UserErrors.Deleted || error == ExpenseErrors.Deleted || error == ExpenseErrors.MonthlyQuotaReached)
                statusCode = StatusCodes.Status409Conflict;

            return Results.Problem(statusCode: statusCode, detail: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                });
        }
    }
}
