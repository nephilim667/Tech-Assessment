namespace WebApi.Infrastructure.Api.ExpenseReports.Contracts
{
    internal sealed record GetExpenseReportsRequest(Guid UserId, int Year = 0, int Month = 0);
}
