using WebApi.Infrastructure.Identity;

using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Endpoints;

namespace WebApi.Infrastructure.Api.Expenses
{
    public static class ExpenseEndpoints
    {
        public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var expenses = endpoints
                .MapGroup("/api/expense-reports/{expenseReportId:guid}/expenses")
                .WithTags("Expenses")
                .RequireAuthorization(AuthorizationPolicies.Administrators);

            expenses.MapGet("", GetExpensesEndpoint.GetAsync)
                    .AddEndpointFilter<ValidationFilter<GetExpensesRequest>>()
                    .Produces<ExpensePageResponse>()
                    .ProducesValidationProblem()
                    .ProducesProblem(StatusCodes.Status404NotFound)
                    .Produces(StatusCodes.Status401Unauthorized)
                    .Produces(StatusCodes.Status403Forbidden);

            expenses.MapPost("", CreateExpenseEndpoint.CreateAsync)
                .AddEndpointFilter<ValidationFilter<CreateExpenseRequest>>()
                .Produces<ExpenseResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return endpoints;
        }
    }
}
