using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.ExpenseReports
{
    internal interface IExpenseReportRepository
    {
        Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExpenseReport>> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
        Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default);
    }
}
