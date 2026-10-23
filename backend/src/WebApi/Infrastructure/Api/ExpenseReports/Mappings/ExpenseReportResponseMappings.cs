using WebApi.Domain.Models;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;

namespace WebApi.Infrastructure.Api.ExpenseReports.Mappings
{
    internal static class ExpenseReportResponseMappings
    {
        public static ExpenseReportResponse ToResponse(this ExpenseReport report) =>
            new(report.Id, report.UserId, report.Title, report.Year, report.Month);
    }
}
