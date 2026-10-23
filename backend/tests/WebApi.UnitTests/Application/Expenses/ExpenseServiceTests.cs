using NSubstitute;

using WebApi.Domain.Models;
using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Expenses;

using WebApi.Application.Expenses;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Application.Interfaces.Expenses;
using WebApi.Application.Interfaces.Users;

namespace WebApi.UnitTests.Application.Expenses
{
    public sealed class ExpenseServiceTests
    {
        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly IExpenseReportRepository _reports = Substitute.For<IExpenseReportRepository>();
        private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        public ExpenseServiceTests()
        {
            _unitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task<Result<Expense>>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                    call.ArgAt<Func<CancellationToken, Task<Result<Expense>>>>(0)(
                        call.ArgAt<CancellationToken>(1)));
        }

        [Fact]
        public async Task CreateAsync_When_Quota_Has_One_Remaining_Slot_Persists_Expense()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            _expenses.CountByUserAndMonthAsync(user.Id, 2026, 10, token).Returns(Task.FromResult(4));

            // Act
            var result = await CreateService().CreateAsync(report.Id, CreateDetails(), token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(report.Id, result.Value.ExpenseReportId);

            await _unitOfWork.Received(1).ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Result<Expense>>>>(),
                token);

            await _users.Received(1).GetByIdForUpdateAsync(user.Id, token);
            await _reports.Received(2).GetByIdAsync(report.Id, token);
            await _expenses.Received(1).AddAsync(result.Value, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        public async Task CreateAsync_When_Quota_Is_Reached_Does_Not_Write(int count)
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            _expenses.CountByUserAndMonthAsync(user.Id, 2026, 10, token).Returns(Task.FromResult(count));

            // Act
            var result = await CreateService().CreateAsync(report.Id, CreateDetails(), token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.monthly_quota_reached", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task CreateAsync_When_Report_Is_Missing_Does_Not_Lock_Or_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var reportId = Guid.NewGuid();

            _reports.GetByIdAsync(reportId, token).Returns(Task.FromResult<ExpenseReport?>(null));

            // Act
            var result = await CreateService().CreateAsync(reportId, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await _users.DidNotReceive().GetByIdForUpdateAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task CreateAsync_When_User_Disappears_Before_Lock_Does_Not_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(null));

            // Act
            var result = await CreateService().CreateAsync(report.Id, CreateDetails(), token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task CreateAsync_When_User_Is_Inactive_Does_Not_Count_Or_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);
            user.Deactivate();

            // Act
            var result = await CreateService().CreateAsync(report.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.inactive", result.Error.Code);

            await _expenses.DidNotReceive().CountByUserAndMonthAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task CreateAsync_When_Report_Disappears_During_Lock_Wait_Does_Not_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (_, report) = PrepareUserAndReport(token);

            _reports.GetByIdAsync(report.Id, token)
                .Returns(
                    Task.FromResult<ExpenseReport?>(report),
                    Task.FromResult<ExpenseReport?>(null));

            // Act
            var result = await CreateService().CreateAsync(report.Id, CreateDetails(), token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task CreateAsync_When_Expense_Is_Invalid_Does_Not_Write()
        {
            // Arrange
            var token = TestContext.Current.CancellationToken;
            var (_, report) = PrepareUserAndReport(token);

            // Act
            var details = CreateDetails() with { Amount = 0m };
            var result = await CreateService().CreateAsync(report.Id, details, token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_amount", result.Error.Code);

            await AssertNoWriteAsync();
        }


        // Helpers
        private (User User, ExpenseReport Report) PrepareUserAndReport(CancellationToken token)
        {
            var user = User.Create(
                "Juste", "Leblanc",
                "10 rue des Lilas", "75001", "Paris", 5).Value;

            var report = ExpenseReport.Create(user, 2026, 10).Value;

            _reports.GetByIdAsync(report.Id, token).Returns(Task.FromResult<ExpenseReport?>(report));
            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(user));

            return (user, report);
        }

        private async Task AssertNoWriteAsync()
        {
            await _expenses.DidNotReceive().AddAsync(
                Arg.Any<Expense>(),
                Arg.Any<CancellationToken>());

            await _unitOfWork.DidNotReceive().SaveChangesAsync(
                Arg.Any<CancellationToken>());
        }

        private ExpenseService CreateService() => new(_users, _reports, _expenses, _unitOfWork);

        private static ExpenseDetails CreateDetails() =>
            new(
                Date: new DateOnly(2026, 10, 15),
                Description: "Déjeuner",
                Amount: 25.50m,
                BillingStreet: "20 rue du Restaurant",
                BillingPostalCode: "69001",
                BillingCity: "Lyon");
    }
}
