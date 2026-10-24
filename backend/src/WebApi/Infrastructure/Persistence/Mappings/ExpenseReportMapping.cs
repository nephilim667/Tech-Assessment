using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence.Entities;

namespace WebApi.Infrastructure.Persistence.Mappings
{
    internal static class ExpenseReportMapping
    {
        public static ExpenseReportEntity ToEntity(this ExpenseReport report)
        {
            return new ExpenseReportEntity
            {
                Id = report.Id,
                UserId = report.UserId,
                Title = report.Title,
                Year = report.Year,
                Month = report.Month,
                Number = report.Number,
            };
        }

        public static ExpenseReport ToDomain(this ExpenseReportEntity entity)
        {
            return ExpenseReport.Restore(
                id: entity.Id,
                userId: entity.UserId,
                title: entity.Title,
                year: entity.Year,
                month: entity.Month,
                number: entity.Number);
        }
    }
}
