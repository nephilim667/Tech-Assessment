using WebApi.Domain.Models;

using WebApi.Application.Expenses.Contracts;

namespace WebApi.Application.Interfaces.Expenses
{
    internal interface IExpenseRepository
    {
        Task<ExpensePage> GetPageAsync(Guid expenseReportId, int pageNumber, CancellationToken cancellationToken = default);
        Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
        Task UpdateStateAsync(Expense expense, CancellationToken cancellationToken = default);
        Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default);
        Task<int> CountByUserAndMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    }
}
