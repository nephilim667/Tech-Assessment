using WebApi.Infrastructure.Identity;

using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Endpoints;

namespace WebApi.Infrastructure.Api.ExpenseReports
{
    public static class ExpenseReportEndpoints
    {
        public static IEndpointRouteBuilder MapExpenseReportEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var reports = endpoints
                .MapGroup("/api/expense-reports")
                .WithTags("Expense reports")
                .RequireAuthorization(AuthorizationPolicies.Administrators);

            reports.MapPost("", CreateExpenseReportEndpoint.CreateAsync)
                   .AddEndpointFilter<ValidationFilter<CreateExpenseReportRequest>>()
                   .Produces<ExpenseReportResponse>(StatusCodes.Status201Created)
                   .ProducesValidationProblem()
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status409Conflict)
                   .Produces(StatusCodes.Status401Unauthorized)
                   .Produces(StatusCodes.Status403Forbidden);

            return endpoints;
        }
    }
}
