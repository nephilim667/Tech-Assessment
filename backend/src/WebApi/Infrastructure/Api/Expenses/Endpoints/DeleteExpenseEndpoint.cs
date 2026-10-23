using WebApi.Application.Interfaces.Expenses;
using WebApi.Domain.Errors;

namespace WebApi.Infrastructure.Api.Expenses.Endpoints
{
    internal static class DeleteExpenseEndpoint
    {
        public static async Task<IResult> DeleteAsync(Guid expenseReportId, Guid expenseId, IExpenseService service, CancellationToken cancellationToken)
        {
            var result = await service.DeleteAsync(expenseReportId, expenseId, cancellationToken);

            if (result.IsSuccess)
                return Results.NoContent();

            var statusCode = result.Error == ExpenseErrors.Deleted
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status404NotFound;

            return Results.Problem(
                statusCode: statusCode,
                detail: result.Error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                });
        }
    }
}
