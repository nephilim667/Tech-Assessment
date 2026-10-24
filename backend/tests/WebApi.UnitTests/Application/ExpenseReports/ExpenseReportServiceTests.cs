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

        [Fact]
        public async Task GetByIdAsync_When_Report_Exists_Returns_Report()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var report = ExpenseReport.Create(CreateUser(), 2026, 10).Value;

            _reports.GetByIdAsync(report.Id, token).Returns(Task.FromResult<ExpenseReport?>(report));

            var service = CreateService();

            // Act
            var result = await service.GetByIdAsync(report.Id, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(report, result.Value);
        }

        [Fact]
        public async Task GetByIdAsync_When_Report_Is_Missing_Returns_NotFound()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var id = Guid.NewGuid();

            _reports.GetByIdAsync(id, token).Returns(Task.FromResult<ExpenseReport?>(null));

            var service = CreateService();

            // Act
            var result = await service.GetByIdAsync(id, token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetByUserAndPeriodAsync_Allows_Active_Or_Inactive_User(bool isActive)
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            var report = ExpenseReport.Create(user, 2026, 10).Value;

            if (!isActive)
                user.Deactivate();

            IReadOnlyList<ExpenseReport> expected = [report];

            _users.GetByIdAsync(user.Id, token).Returns(Task.FromResult<User?>(user));
            _reports.GetByUserAndPeriodAsync(user.Id, 2026, 10, token).Returns(Task.FromResult(expected));

            var result = await CreateService().GetByUserAndPeriodAsync(user.Id, 2026, 10, token);

            Assert.True(result.IsSuccess);
            Assert.Same(expected, result.Value);

            await _reports.Received(1).GetByUserAndPeriodAsync(user.Id, 2026, 10, token);
        }

        [Fact]
        public async Task GetByUserAndPeriodAsync_When_User_Is_Missing_Returns_NotFound()
        {
            var token = TestContext.Current.CancellationToken;
            var userId = Guid.NewGuid();

            _users.GetByIdAsync(userId, token).Returns(Task.FromResult<User?>(null));

            var result = await CreateService().GetByUserAndPeriodAsync(userId, 2026, 10, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await _reports.DidNotReceive().GetByUserAndPeriodAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10000, 10)]
        [InlineData(2026, 0)]
        [InlineData(2026, 13)]
        public async Task GetByUserAndPeriodAsync_When_Period_Is_Invalid_Does_Not_Query(int year, int month)
        {
            var token = TestContext.Current.CancellationToken;

            var result = await CreateService().GetByUserAndPeriodAsync(Guid.NewGuid(), year, month, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.invalid_period", result.Error.Code);

            await _users.DidNotReceive().GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

            await _reports.DidNotReceive().GetByUserAndPeriodAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeleteAsync_Allows_Active_Or_Inactive_User(bool isActive)
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            var report = ExpenseReport.Create(user, 2026, 10).Value;

            if (!isActive)
                user.Deactivate();

            _reports.GetByIdAsync(report.Id, token).Returns(Task.FromResult<ExpenseReport?>(report));
            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(user));

            var result = await CreateService().DeleteAsync(report.Id, token);

            Assert.True(result.IsSuccess);
            Assert.Same(report, result.Value);

            await _unitOfWork.Received(1).ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Result<ExpenseReport>>>>(),
                token);

            await _users.Received(1).GetByIdForUpdateAsync(user.Id, token);
            await _reports.Received(2).GetByIdAsync(report.Id, token);
            await _reports.Received(1).DeleteAsync(report, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeleteAsync_When_Report_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var id = Guid.NewGuid();

            _reports.GetByIdAsync(id, token).Returns(Task.FromResult<ExpenseReport?>(null));

            var result = await CreateService().DeleteAsync(id, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await _users.DidNotReceive().GetByIdForUpdateAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

            await AssertNoReportDeletionAsync();
        }

        [Fact]
        public async Task DeleteAsync_When_User_Disappears_Before_Lock_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            var report = ExpenseReport.Create(user, 2026, 10).Value;

            _reports.GetByIdAsync(report.Id, token).Returns(Task.FromResult<ExpenseReport?>(report));
            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(null));

            var result = await CreateService().DeleteAsync(report.Id, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await AssertNoReportDeletionAsync();
        }

        [Fact]
        public async Task DeleteAsync_When_Report_Disappears_During_Lock_Wait_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var user = CreateUser();
            var report = ExpenseReport.Create(user, 2026, 10).Value;

            _reports.GetByIdAsync(report.Id, token).Returns(Task.FromResult<ExpenseReport?>(report), Task.FromResult<ExpenseReport?>(null));
            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(user));

            var result = await CreateService().DeleteAsync(report.Id, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await AssertNoReportDeletionAsync();
        }

        private async Task AssertNoReportDeletionAsync()
        {
            await _reports.DidNotReceive().DeleteAsync(
                Arg.Any<ExpenseReport>(),
                Arg.Any<CancellationToken>());

            await _unitOfWork.DidNotReceive().SaveChangesAsync(
                Arg.Any<CancellationToken>());
        }

        // Helpers
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
