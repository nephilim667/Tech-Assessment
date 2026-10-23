using WebApi.Domain.Models;

namespace WebApi.Application.Interfaces.Expenses
{
    internal interface IExpenseRepository
    {
        Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
        Task<int> CountByUserAndMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    }
}
