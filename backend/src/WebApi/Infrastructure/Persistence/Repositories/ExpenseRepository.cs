using Microsoft.EntityFrameworkCore;

using WebApi.Application.Interfaces.Expenses;

using WebApi.Domain.Models;

using WebApi.Infrastructure.Persistence.Mappings;

namespace WebApi.Infrastructure.Persistence.Repositories
{
    internal sealed class ExpenseRepository(AppDbContext context) : IExpenseRepository
    {
        public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            await context.Expenses.AddAsync(expense.ToEntity(), cancellationToken);
        }

        public Task<int> CountByUserAndMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
        {
            var query =
                from expense in context.Expenses
                join report in context.ExpenseReports
                    on expense.ExpenseReportId equals report.Id
                where report.UserId == userId
                    && report.Year == year
                    && report.Month == month
                    && !expense.IsDeleted
                select expense.Id;

            return query.CountAsync(cancellationToken);
        }
    }
}
