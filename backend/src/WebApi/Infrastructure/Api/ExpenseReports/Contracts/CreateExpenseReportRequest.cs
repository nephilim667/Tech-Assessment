namespace WebApi.Infrastructure.Api.ExpenseReports.Contracts
{
    internal sealed record CreateExpenseReportRequest(Guid UserId, int Year, int Month);
}