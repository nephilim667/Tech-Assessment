using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.Expenses;
using WebApi.Infrastructure.Api.Expenses.Mappings;
using WebApi.Infrastructure.Api.Expenses.Contracts;

namespace WebApi.Infrastructure.Api.Expenses.Endpoints
{
    internal static class GetExpensesEndpoint
    {
        public static async Task<IResult> GetAsync(Guid expenseReportId, [AsParameters] GetExpensesRequest request, IExpenseService service, CancellationToken cancellationToken)
        {
            var result = await service.GetPageAsync(
                expenseReportId,
                request.Page,
                cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value.ToResponse());

            var statusCode = result.Error == ExpenseReportErrors.NotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

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
