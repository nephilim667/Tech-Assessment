using WebApi.Domain.Models;
using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Expenses;

using WebApi.Application.Expenses.Contracts;

namespace WebApi.Application.Interfaces.Expenses
{
    internal interface IExpenseService
    {
        Task<Result<Expense>> CreateAsync(Guid expenseReportId, ExpenseDetails details, CancellationToken cancellationToken = default);
        Task<Result<ExpensePage>> GetPageAsync(Guid expenseReportId, int pageNumber, CancellationToken cancellationToken = default);
        Task<Result<Expense>> DeleteAsync(Guid expenseReportId, Guid expenseId, CancellationToken cancellationToken = default);
    }
}
