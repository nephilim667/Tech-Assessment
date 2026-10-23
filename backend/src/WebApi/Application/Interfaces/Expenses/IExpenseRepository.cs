using WebApi.Domain.Models;

using WebApi.Application.Expenses.Contracts;

namespace WebApi.Application.Interfaces.Expenses
{
    internal interface IExpenseRepository
    {
        Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
        Task<int> CountByUserAndMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
        Task<ExpensePage> GetPageAsync(Guid expenseReportId, int pageNumber, CancellationToken cancellationToken = default);
    }
}
