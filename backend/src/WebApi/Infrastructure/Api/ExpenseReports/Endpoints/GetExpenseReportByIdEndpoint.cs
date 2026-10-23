using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Infrastructure.Api.ExpenseReports.Mappings;

namespace WebApi.Infrastructure.Api.ExpenseReports.Endpoints
{
    internal static class GetExpenseReportByIdEndpoint
    {
        public static async Task<IResult> GetAsync(Guid id, IExpenseReportService service, CancellationToken cancellationToken)
        {
            var result = await service.GetByIdAsync(id, cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value.ToResponse());

            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.Error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                });
        }
    }
}
