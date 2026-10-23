namespace WebApi.Infrastructure.Api.ExpenseReports.Contracts
{
    internal sealed record ExpenseReportResponse(Guid Id, Guid UserId, string Title, int Year, int Month);
}