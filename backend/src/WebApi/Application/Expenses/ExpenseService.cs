using WebApi.Domain.Common;
using WebApi.Domain.Errors;
using WebApi.Domain.Models;
using WebApi.Domain.Contracts.Expenses;

using WebApi.Application.Common.Errors;
using WebApi.Application.Expenses.Contracts;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Expenses;
using WebApi.Application.Interfaces.ExpenseReports;

namespace WebApi.Application.Expenses
{
    internal sealed class ExpenseService(IUserRepository users, IExpenseReportRepository reports, IExpenseRepository expenses, IUnitOfWork unitOfWork) : IExpenseService
    {
        public Task<Result<Expense>> CreateAsync(Guid expenseReportId, ExpenseDetails details, CancellationToken cancellationToken = default)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var report = await reports.GetByIdAsync(expenseReportId, token);

                    if (report is null)
                        return Result<Expense>.Failure(ExpenseReportErrors.NotFound);

                    var user = await users.GetByIdForUpdateAsync(report.UserId, token);

                    if (user is null)
                        return Result<Expense>.Failure(UserErrors.NotFound);

                    if (!user.IsActive)
                        return Result<Expense>.Failure(UserErrors.Inactive);

                    // La note a pu être supprimée pendant l’attente du verrou.
                    report = await reports.GetByIdAsync(expenseReportId, token);

                    if (report is null)
                        return Result<Expense>.Failure(ExpenseReportErrors.NotFound);

                    var result = Expense.Create(report, details);

                    if (!result.IsSuccess)
                        return result;

                    var count = await expenses.CountByUserAndMonthAsync(user.Id, report.Year, report.Month, token);

                    if (count >= user.MaxExpensesPerMonth)
                    {
                        return Result<Expense>.Failure(
                            ExpenseErrors.MonthlyQuotaReached);
                    }

                    await expenses.AddAsync(result.Value, token);
                    await unitOfWork.SaveChangesAsync(token);

                    return result;
                },
                cancellationToken);
        }

        public async Task<Result<ExpensePage>> GetPageAsync(Guid expenseReportId, int pageNumber, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
                return Result<ExpensePage>.Failure(PaginationErrors.InvalidPage);

            var report = await reports.GetByIdAsync(expenseReportId, cancellationToken);

            if (report is null)
                return Result<ExpensePage>.Failure(ExpenseReportErrors.NotFound);

            var page = await expenses.GetPageAsync(expenseReportId, pageNumber,cancellationToken);

            return Result<ExpensePage>.Success(page);
        }
    }
}
