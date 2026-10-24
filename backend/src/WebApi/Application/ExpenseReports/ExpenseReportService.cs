using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Application.Interfaces.Users;

using WebApi.Domain.Common;
using WebApi.Domain.Errors;
using WebApi.Domain.Models;

namespace WebApi.Application.ExpenseReports
{
    internal sealed class ExpenseReportService(IUserRepository userRepository, IExpenseReportRepository reportRepository, IUnitOfWork unitOfWork) : IExpenseReportService
    {
        public async Task<Result<ExpenseReport>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var report = await reportRepository.GetByIdAsync(id, cancellationToken);

            return report is null
                ? Result<ExpenseReport>.Failure(ExpenseReportErrors.NotFound)
                : Result<ExpenseReport>.Success(report);
        }

        public async Task<Result<IReadOnlyList<ExpenseReport>>> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
        {
            if (year is < 1 or > 9999 || month is < 1 or > 12)
                return Result<IReadOnlyList<ExpenseReport>>.Failure(ExpenseReportErrors.InvalidPeriod);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
                return Result<IReadOnlyList<ExpenseReport>>.Failure(UserErrors.NotFound);

            var reports = await reportRepository.GetByUserAndPeriodAsync(userId, year, month, cancellationToken);

            return Result<IReadOnlyList<ExpenseReport>>.Success(reports);
        }

        public Task<Result<ExpenseReport>> CreateAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var user = await userRepository.GetByIdForUpdateAsync(userId, token);

                    if (user is null)
                        return Result<ExpenseReport>.Failure(UserErrors.NotFound);

                    var result = ExpenseReport.Create(user, year, month);

                    if (!result.IsSuccess)
                        return result;

                    await reportRepository.AddAsync(result.Value, token);
                    await unitOfWork.SaveChangesAsync(token);

                    return result;
                },
                cancellationToken);
        }
    }
}
