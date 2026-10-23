using WebApi.Domain.Common;
using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.ExpenseReports
{
    internal interface IExpenseReportService
    {
        Task<Result<ExpenseReport>> CreateAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    }
}
