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
