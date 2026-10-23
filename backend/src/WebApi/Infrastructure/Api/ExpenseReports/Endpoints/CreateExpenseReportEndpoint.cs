using WebApi.Domain.Errors;

using WebApi.Application.Interfaces.ExpenseReports;

using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Mappings;

namespace WebApi.Infrastructure.Api.ExpenseReports.Endpoints
{
    internal static class CreateExpenseReportEndpoint
    {
        public static async Task<IResult> CreateAsync(CreateExpenseReportRequest request, IExpenseReportService service, CancellationToken cancellationToken)
        {
            var result = await service.CreateAsync(
                request.UserId,
                request.Year,
                request.Month,
                cancellationToken);

            if (result.IsSuccess)
            {
                return Results.CreatedAtRoute(
                    routeName: ExpenseReportEndpoints.GetByIdRouteName,
                    routeValues: new { id = result.Value.Id },
                    value: result.Value.ToResponse());
            }

            var error = result.Error;
            var inactiveOrDeletedCode = error == UserErrors.Inactive || error == UserErrors.Deleted ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
            var statusCode = error == UserErrors.NotFound ? StatusCodes.Status404NotFound : inactiveOrDeletedCode;

            return Results.Problem(
                statusCode: statusCode,
                detail: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                });
        }
    }
}
