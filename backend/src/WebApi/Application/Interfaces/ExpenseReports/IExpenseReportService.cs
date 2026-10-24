using WebApi.Domain.Common;
using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.ExpenseReports
{
    internal interface IExpenseReportService
    {
        Task<Result<ExpenseReport>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<ExpenseReport>>> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
        Task<Result<ExpenseReport>> CreateAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
        Task<Result<ExpenseReport>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
