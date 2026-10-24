using Microsoft.EntityFrameworkCore;

using WebApi.Domain.Models;

using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Application.Interfaces.ExpenseReports;

namespace WebApi.Infrastructure.Persistence.Repositories
{
    internal sealed class ExpenseReportRepository(AppDbContext context) : IExpenseReportRepository
    {
        public async Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await context.ExpenseReports
                .AsNoTracking()
                .SingleOrDefaultAsync(report => report.Id == id && context.Beneficiaries.Any(user => user.Id == report.UserId && !user.IsDeleted), cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default)
        {
            await context.ExpenseReports.AddAsync(report.ToEntity(), cancellationToken);
        }

        public async Task<IReadOnlyList<ExpenseReport>> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
        {
            var query = from report in context.ExpenseReports
                        join user in context.Beneficiaries
                            on report.UserId equals user.Id
                        where report.UserId == userId
                            && report.Year == year
                            && report.Month == month
                            && !user.IsDeleted
                        orderby report.Id
                        select report;

            var entities = await query.AsNoTracking().ToListAsync(cancellationToken);

            return entities.Select(entity => entity.ToDomain()).ToArray();
        }

        public async Task DeleteAsync(ExpenseReport report, CancellationToken cancellationToken = default)
        {
            var entity = await context.ExpenseReports.SingleAsync(
                entity => entity.Id == report.Id,
                cancellationToken);

            context.ExpenseReports.Remove(entity);
        }
    }
}
