using WebApi.Domain.Models;

namespace WebApi.UnitTests.Domain.ExpenseReports
{
    public sealed class ExpenseReportTests
    {
        [Fact]
        public void Create_When_User_Is_Active_Creates_Report_With_Frozen_Title()
        {
            // Arrange
            var user = CreateUser();

            // Act
            var result = ExpenseReport.Create(user, year: 2026, month: 10);

            // Assert
            Assert.True(result.IsSuccess);

            var report = result.Value;

            Assert.NotEqual(Guid.Empty, report.Id);
            Assert.Equal(user.Id, report.UserId);
            Assert.Equal(2026, report.Year);
            Assert.Equal(10, report.Month);
            Assert.Equal("Leblanc Juste - octobre 2026", report.Title);
        }

        [Fact]
        public void Create_When_User_Is_Inactive_Returns_Failure()
        {
            // Arrange
            var user = CreateUser();
            user.Deactivate();

            // Act
            var result = ExpenseReport.Create(user, year: 2026, month: 10);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.inactive", result.Error.Code);
        }

        [Fact]
        public void Create_When_User_Is_Deleted_Returns_Failure()
        {
            // Arrange
            var user = CreateUser();
            user.Delete();

            // Act
            var result = ExpenseReport.Create(user, year: 2026, month: 10);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.deleted", result.Error.Code);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10000, 10)]
        [InlineData(2026, 0)]
        [InlineData(2026, 13)]
        public void Create_When_Period_Is_Invalid_Returns_Failure(int year, int month)
        {
            // Act
            var result = ExpenseReport.Create(CreateUser(), year, month);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense_report.invalid_period", result.Error.Code);
        }

        [Fact]
        public void Create_When_User_Already_Has_Report_For_Same_Month_Creates_Another_Report()
        {
            // Arrange
            var user = CreateUser();

            // Act
            var first = ExpenseReport.Create(user, year: 2026, month: 10);
            var second = ExpenseReport.Create(user, year: 2026, month: 10);

            // Assert
            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            Assert.NotEqual(first.Value.Id, second.Value.Id);
            Assert.Equal(first.Value.Title, second.Value.Title);
        }

        private static User CreateUser() =>
            User.Create(
                firstName: "Juste",
                lastName: "Leblanc",
                street: "10 rue des Lilas",
                postalCode: "75001",
                city: "Paris",
                maxExpensesPerMonth: 5).Value;
    }
}
