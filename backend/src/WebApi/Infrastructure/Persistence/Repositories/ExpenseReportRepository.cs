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
    }
}
