using Microsoft.EntityFrameworkCore;

using WebApi.Application.Expenses;
using WebApi.Application.Expenses.Contracts;
using WebApi.Application.Interfaces.Expenses;
using WebApi.Application.Common.Pagination;

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

        public async Task<ExpensePage> GetPageAsync(Guid expenseReportId, int pageNumber, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

            var query = from expense in context.Expenses
                        join report in context.ExpenseReports
                            on expense.ExpenseReportId equals report.Id
                        join user in context.Beneficiaries
                            on report.UserId equals user.Id
                        where report.Id == expenseReportId
                            && !expense.IsDeleted
                            && !user.IsDeleted
                        select expense;

            var summary = await query
                .GroupBy(expense => 1)
                .Select(group => new
                {
                    Count = group.Count(),
                    Total = group.Sum(expense => expense.Amount)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var totalCount = summary?.Count ?? 0;
            var totalAmount = summary?.Total ?? 0m;

            var offset = ((long)pageNumber - 1) * ExpensePagination.PageSize;

            IReadOnlyList<Expense> items = Array.Empty<Expense>();

            if (offset < totalCount)
            {
                var entities = await query
                    .AsNoTracking()
                    .OrderByDescending(expense => expense.Date)
                    .ThenBy(expense => expense.Id)
                    .Skip((int)offset)
                    .Take(ExpensePagination.PageSize)
                    .ToListAsync(cancellationToken);

                items = entities.Select(entity => entity.ToDomain()).ToArray();
            }

            return new ExpensePage(
                new PagedResult<Expense>(
                    Items: items,
                    PageNumber: pageNumber,
                    PageSize: ExpensePagination.PageSize,
                    TotalCount: totalCount),
                    TotalAmount: totalAmount);
        }

        public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = from expense in context.Expenses
                        join report in context.ExpenseReports
                            on expense.ExpenseReportId equals report.Id
                        join user in context.Beneficiaries
                            on report.UserId equals user.Id
                        where expense.Id == id
                            && !expense.IsDeleted
                            && !user.IsDeleted
                        select expense;

            var entity = await query
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task UpdateStateAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            var entity = await context.Expenses.SingleAsync(
                entity => entity.Id == expense.Id && !entity.IsDeleted,
                cancellationToken);

            expense.ApplyStateTo(entity);
        }

        public async Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            var entity = await context.Expenses.SingleAsync(
                entity => entity.Id == expense.Id && !entity.IsDeleted,
                cancellationToken);

            expense.ApplyChangesTo(entity);
        }
    }
}