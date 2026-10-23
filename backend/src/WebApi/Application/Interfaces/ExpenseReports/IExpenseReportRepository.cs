using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.ExpenseReports
{
    internal interface IExpenseReportRepository
    {
        Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default);
    }
}
