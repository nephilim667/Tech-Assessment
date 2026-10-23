using WebApi.Domain.Models;

using WebApi.Infrastructure.Persistence.Mappings;
using WebApi.Application.Interfaces.ExpenseReports;

namespace WebApi.Infrastructure.Persistence.Repositories
{
    internal sealed class ExpenseReportRepository(AppDbContext context) : IExpenseReportRepository
    {
        public async Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default)
        {
            await context.ExpenseReports.AddAsync(report.ToEntity(), cancellationToken);
        }
    }
}
