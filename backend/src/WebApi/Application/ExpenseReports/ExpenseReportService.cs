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

                    var persisted = await reportRepository.GetByIdAsync(result.Value.Id, token);

                    if (persisted is null)
                        throw new InvalidOperationException("La note de frais sauvegardée ne peut pas être relue.");

                    return Result<ExpenseReport>.Success(persisted);
                },
                cancellationToken);
        }

        public Task<Result<ExpenseReport>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var report = await reportRepository.GetByIdAsync(id, token);

                    if (report is null)
                        return Result<ExpenseReport>.Failure(ExpenseReportErrors.NotFound);

                    var user = await userRepository.GetByIdForUpdateAsync(report.UserId, token);

                    if (user is null)
                        return Result<ExpenseReport>.Failure(UserErrors.NotFound);

                    // Une autre opération a pu supprimer la note pendant l’attente.
                    report = await reportRepository.GetByIdAsync(id, token);

                    if (report is null)
                        return Result<ExpenseReport>.Failure(ExpenseReportErrors.NotFound);

                    await reportRepository.DeleteAsync(report, token);
                    await unitOfWork.SaveChangesAsync(token);

                    return Result<ExpenseReport>.Success(report);
                },
                cancellationToken);
        }
    }
}
