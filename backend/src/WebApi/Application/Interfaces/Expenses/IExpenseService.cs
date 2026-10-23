using WebApi.Domain.Models;
using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Expenses;

namespace WebApi.Application.Interfaces.Expenses
{
    internal interface IExpenseService
    {
        Task<Result<Expense>> CreateAsync(Guid expenseReportId, ExpenseDetails details, CancellationToken cancellationToken = default);
    }
}
