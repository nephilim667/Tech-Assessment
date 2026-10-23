using NSubstitute;

using WebApi.Domain.Models;
using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Expenses;

using WebApi.Application.Common.Pagination;
using WebApi.Application.Expenses;
using WebApi.Application.Expenses.Contracts;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Expenses;
using WebApi.Application.Interfaces.ExpenseReports;

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

        [Fact]
        public async Task GetPageAsync_When_Report_Exists_Returns_Requested_Page()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, report) = PrepareUserAndReport(token);

            var expected = new ExpensePage(
                new PagedResult<Expense>(
                    Items: Array.Empty<Expense>(),
                    PageNumber: 2,
                    PageSize: 5,
                    TotalCount: 0),
                TotalAmount: 0m);

            _expenses.GetPageAsync(report.Id, 2, token).Returns(Task.FromResult(expected));

            var result = await CreateService().GetPageAsync(report.Id, 2, token);

            Assert.True(result.IsSuccess);
            Assert.Same(expected, result.Value);

            await _reports.Received(1).GetByIdAsync(report.Id, token);
            await _expenses.Received(1).GetPageAsync(report.Id, 2, token);
        }

        [Fact]
        public async Task GetPageAsync_When_Report_Is_Missing_Does_Not_Query_Expenses()
        {
            var token = TestContext.Current.CancellationToken;
            var reportId = Guid.NewGuid();

            _reports.GetByIdAsync(reportId, token).Returns(Task.FromResult<ExpenseReport?>(null));

            var result = await CreateService().GetPageAsync(reportId, 1, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await _expenses.DidNotReceive().GetPageAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPageAsync_When_Page_Is_Invalid_Does_Not_Query_Repositories(int pageNumber)
        {
            var token = TestContext.Current.CancellationToken;

            var result = await CreateService().GetPageAsync(Guid.NewGuid(), pageNumber, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("pagination.invalid_page", result.Error.Code);

            await _reports.DidNotReceive().GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

            await _expenses.DidNotReceive().GetPageAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeleteAsync_When_User_Is_Active_Or_Inactive_Persists_Deletion(bool isActive)
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            if (!isActive)
                user.Deactivate();

            var expense = Expense.Create(report, CreateDetails()).Value;

            _expenses.GetByIdAsync(expense.Id, token).Returns(Task.FromResult<Expense?>(expense));

            var result = await CreateService().DeleteAsync(report.Id, expense.Id, token);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsDeleted);

            await _unitOfWork.Received(1).ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Result<Expense>>>>(),
                token);

            await _users.Received(1).GetByIdForUpdateAsync(user.Id, token);
            await _expenses.Received(1).UpdateStateAsync(expense, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeleteAsync_When_Report_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var reportId = Guid.NewGuid();

            _reports.GetByIdAsync(reportId, token).Returns(Task.FromResult<ExpenseReport?>(null));

            var result = await CreateService().DeleteAsync(reportId, Guid.NewGuid(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await _users.DidNotReceive().GetByIdForUpdateAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task DeleteAsync_When_User_Disappears_Before_Lock_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(null));

            var result = await CreateService().DeleteAsync(report.Id, Guid.NewGuid(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task DeleteAsync_When_Expense_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, report) = PrepareUserAndReport(token);
            var expenseId = Guid.NewGuid();

            _expenses.GetByIdAsync(expenseId, token)
                .Returns(Task.FromResult<Expense?>(null));

            var result = await CreateService().DeleteAsync(report.Id, expenseId, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task DeleteAsync_When_Expense_Belongs_To_Another_Report_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, report) = PrepareUserAndReport(token);

            var otherReport = ExpenseReport.Create(user, 2026, 10).Value;
            var expense = Expense.Create(otherReport, CreateDetails()).Value;

            _expenses.GetByIdAsync(expense.Id, token)
                .Returns(Task.FromResult<Expense?>(expense));

            var result = await CreateService().DeleteAsync(report.Id, expense.Id, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateAsync_Within_Same_Report_Allows_Active_Or_Inactive_User(bool isActive)
        {
            var token = TestContext.Current.CancellationToken;
            var (user, source, expense) = PrepareExpenseForUpdate(token);

            if (!isActive)
                user.Deactivate();

            var details = CreateDetails() with { Amount = 42m };
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, source.Id, details, token);

            Assert.True(result.IsSuccess);
            Assert.Equal(42m, result.Value.Amount);

            await _users.Received(1).GetByIdForUpdateAsync(user.Id, token);

            await _unitOfWork.Received(1).ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Result<Expense>>>>(),
                token);

            await AssertNoQuotaCountAsync();
            await _expenses.Received(1).UpdateAsync(expense, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdateAsync_To_Another_Report_In_Same_Month_Does_Not_Check_Quota()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source, expense) = PrepareExpenseForUpdate(token);

            var destination = PrepareDestination(source, token, month: source.Month);
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destination.Id, CreateDetails(), token);

            Assert.True(result.IsSuccess);
            Assert.Equal(destination.Id, result.Value.ExpenseReportId);

            await AssertNoQuotaCountAsync();
            await _expenses.Received(1).UpdateAsync(expense, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdateAsync_To_Another_Month_Checks_Destination_Quota()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, source, expense) = PrepareExpenseForUpdate(token);
            var destination = PrepareDestination(source, token);

            _expenses.CountByUserAndMonthAsync(user.Id, destination.Year, destination.Month, token).Returns(Task.FromResult(4));

            var details = CreateDetails() with
            {
                Date = new DateOnly(destination.Year, destination.Month, 5)
            };

            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destination.Id, details, token);

            Assert.True(result.IsSuccess);
            Assert.Equal(destination.Id, result.Value.ExpenseReportId);
            Assert.Equal(details.Date, result.Value.Date);

            await _expenses.Received(1).CountByUserAndMonthAsync(user.Id, destination.Year, destination.Month, token);
            await _expenses.Received(1).UpdateAsync(expense, token);
            await _unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        public async Task UpdateAsync_When_Destination_Quota_Is_Reached_Does_Not_Change_Expense(int count)
        {
            var token = TestContext.Current.CancellationToken;
            var (user, source, expense) = PrepareExpenseForUpdate(token);
            var destination = PrepareDestination(source, token);
            var originalDate = expense.Date;

            _expenses.CountByUserAndMonthAsync(user.Id, destination.Year, destination.Month, token).Returns(Task.FromResult(count));

            var details = CreateDetails() with
            {
                Date = new DateOnly(destination.Year, destination.Month, 5)
            };

            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destination.Id, details, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.monthly_quota_reached", result.Error.Code);
            Assert.Equal(source.Id, expense.ExpenseReportId);
            Assert.Equal(originalDate, expense.Date);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Destination_Belongs_To_Another_User_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source, expense) = PrepareExpenseForUpdate(token);

            var destination = PrepareDestination(source, token, userId: Guid.NewGuid());
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destination.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.destination_user_mismatch", result.Error.Code);
            Assert.Equal(source.Id, expense.ExpenseReportId);

            await AssertNoQuotaCountAsync();
            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Inactive_User_Is_Transferred_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, source, expense) = PrepareExpenseForUpdate(token);
            user.Deactivate();

            // Même mois : cela reste un transfert entre deux notes.
            var destination = PrepareDestination(source, token, month: source.Month);
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destination.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.inactive", result.Error.Code);
            Assert.Equal(source.Id, expense.ExpenseReportId);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Destination_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source, expense) = PrepareExpenseForUpdate(token);
            var destinationId = Guid.NewGuid();

            _reports.GetByIdAsync(destinationId, token).Returns(Task.FromResult<ExpenseReport?>(null));

            var result = await CreateService().UpdateAsync(source.Id, expense.Id, destinationId, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Expense_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source) = PrepareUserAndReport(token);
            var expenseId = Guid.NewGuid();

            _expenses.GetByIdAsync(expenseId, token).Returns(Task.FromResult<Expense?>(null));

            var result = await CreateService().UpdateAsync(source.Id, expenseId, source.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Expense_Does_Not_Belong_To_Source_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source, expense) = PrepareExpenseForUpdate(token);

            var otherSource = PrepareDestination(source, token, month: source.Month);
            var result = await CreateService().UpdateAsync(otherSource.Id, expense.Id, source.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Source_Is_Missing_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var sourceId = Guid.NewGuid();

            _reports.GetByIdAsync(sourceId, token).Returns(Task.FromResult<ExpenseReport?>(null));
            var result = await CreateService().UpdateAsync(sourceId, Guid.NewGuid(), sourceId, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_User_Disappears_Before_Lock_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (user, source, expense) = PrepareExpenseForUpdate(token);

            _users.GetByIdForUpdateAsync(user.Id, token).Returns(Task.FromResult<User?>(null));
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, source.Id, CreateDetails(), token);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await AssertNoWriteAsync();
        }

        [Fact]
        public async Task UpdateAsync_When_Details_Are_Invalid_Does_Not_Write()
        {
            var token = TestContext.Current.CancellationToken;
            var (_, source, expense) = PrepareExpenseForUpdate(token);
            var originalAmount = expense.Amount;

            var details = CreateDetails() with { Amount = 0m };
            var result = await CreateService().UpdateAsync(source.Id, expense.Id, source.Id, details, token);

            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_amount", result.Error.Code);
            Assert.Equal(originalAmount, expense.Amount);

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

            await _expenses.DidNotReceive().AddAsync(
                Arg.Any<Expense>(),
                Arg.Any<CancellationToken>());

            await _expenses.DidNotReceive().UpdateStateAsync(
                Arg.Any<Expense>(),
                Arg.Any<CancellationToken>());

            await _unitOfWork.DidNotReceive().SaveChangesAsync(
                Arg.Any<CancellationToken>());

            await _expenses.DidNotReceive().UpdateAsync(
                Arg.Any<Expense>(),
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

        private (User User, ExpenseReport Source, Expense Expense) PrepareExpenseForUpdate(CancellationToken token)
        {
            var (user, report) = PrepareUserAndReport(token);
            var expense = Expense.Create(report, CreateDetails()).Value;

            _expenses.GetByIdAsync(expense.Id, token).Returns(Task.FromResult<Expense?>(expense));
            return (user, report, expense);
        }

        private ExpenseReport PrepareDestination(ExpenseReport source, CancellationToken token, int month = 11, Guid? userId = null)
        {
            var destination = ExpenseReport.Restore(
                Guid.NewGuid(),
                userId ?? source.UserId,
                "Note de destination",
                source.Year,
                month);

            _reports.GetByIdAsync(destination.Id, token).Returns(Task.FromResult<ExpenseReport?>(destination));

            return destination;
        }

        private async Task AssertNoQuotaCountAsync()
        {
            await _expenses.DidNotReceive().CountByUserAndMonthAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }
    }
}
