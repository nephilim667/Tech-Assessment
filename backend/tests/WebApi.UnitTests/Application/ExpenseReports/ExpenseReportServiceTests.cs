using NSubstitute;

using WebApi.Domain.Common;
using WebApi.Domain.Models;
using WebApi.Application.ExpenseReports;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Application.Interfaces.Users;

namespace WebApi.UnitTests.Application.ExpenseReports
{
    public sealed class ExpenseReportServiceTests
    {
        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly IExpenseReportRepository _reports = Substitute.For<IExpenseReportRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        public ExpenseReportServiceTests()
        {
            _unitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task<Result<ExpenseReport>>>>(),
                    Arg.Any<CancellationToken>())
                    .Returns(call => call.ArgAt<Func<CancellationToken, Task<Result<ExpenseReport>>>>(0)(call.ArgAt<CancellationToken>(1)));
        }

        [Fact]
        public async Task CreateAsync_When_User_Is_Active_Persists_Report_In_Transaction()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();

            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(user));

            var service = CreateService();

            // Act
            var result = await service.CreateAsync(user.Id, 2026, 10, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(user.Id, result.Value.UserId);
            Assert.Equal(2026, result.Value.Year);
            Assert.Equal(10, result.Value.Month);

            await _unitOfWork.Received(1).ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task<Result<ExpenseReport>>>>(),
                    token);

            await _users.Received(1).GetByIdForUpdateAsync(user.Id, token);
            await _reports.Received(1).AddAsync(result.Value, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task CreateAsync_When_User_Is_Missing_Does_Not_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var userId = Guid.NewGuid();

            _users.GetByIdForUpdateAsync(userId, token).Returns(Task.FromResult<User?>(null));

            var service = CreateService();

            // Act
            var result = await service.CreateAsync(userId, 2026, 10, token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await _reports.DidNotReceive().AddAsync(Arg.Any<ExpenseReport>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_When_User_Is_Inactive_Does_Not_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            user.Deactivate();

            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(user));

            var service = CreateService();

            // Act
            var result = await service.CreateAsync(user.Id, 2026, 10, token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.inactive", result.Error.Code);

            await _reports.DidNotReceive().AddAsync(Arg.Any<ExpenseReport>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        private ExpenseReportService CreateService() => new(_users, _reports, _unitOfWork);

        private static User CreateUser() =>
            User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;
    }
}
