using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Mappings;

namespace WebApi.Infrastructure.Api.ExpenseReports.Endpoints
{
    internal static class GetExpenseReportsEndpoint
    {
        public static async Task<IResult> GetAsync([AsParameters] GetExpenseReportsRequest request, IExpenseReportService service, CancellationToken cancellationToken)
        {
            var result = await service.GetByUserAndPeriodAsync(request.UserId, request.Year, request.Month, cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value.Select(report => report.ToResponse()).ToArray());
            }

            var statusCode = result.Error == UserErrors.NotFound
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
