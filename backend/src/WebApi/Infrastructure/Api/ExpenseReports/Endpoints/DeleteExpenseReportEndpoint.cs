using WebApi.Application.Interfaces.ExpenseReports;

namespace WebApi.Infrastructure.Api.ExpenseReports.Endpoints
{
    internal static class DeleteExpenseReportEndpoint
    {
        public static async Task<IResult> DeleteAsync(Guid id, IExpenseReportService service, CancellationToken cancellationToken)
        {
            var result = await service.DeleteAsync(id, cancellationToken);

            if (result.IsSuccess)
                return Results.NoContent();

            return Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: result.Error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                });
        }
    }
}
