using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Validators;

namespace WebApi.UnitTests.Api.ExpenseReports
{
    public sealed class CreateExpenseReportRequestValidatorTests
    {
        private readonly CreateExpenseReportRequestValidator _validator = new();

        [Theory]
        [InlineData(1, 1)]
        [InlineData(9999, 12)]
        [InlineData(2026, 10)]
        public void Validate_When_Request_Is_Valid_Returns_No_Error(int year,int month)
        {
            // Arrange
            var request = new CreateExpenseReportRequest(Guid.NewGuid(), year, month);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_When_UserId_Is_Empty_Returns_Error()
        {
            // Arrange
            var request = new CreateExpenseReportRequest(Guid.Empty, 2026, 10);

            // Act
            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            // Assert
            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(CreateExpenseReportRequest.UserId), error.PropertyName);
            Assert.Equal("expense_report.user_required", error.ErrorCode);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10000)]
        public void Validate_When_Year_Is_Invalid_Returns_Error(int year)
        {
            // Arrange
            var request = new CreateExpenseReportRequest(Guid.NewGuid(), year, 10);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(CreateExpenseReportRequest.Year), error.PropertyName);
            Assert.Equal("expense_report.invalid_period", error.ErrorCode);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(13)]
        public void Validate_When_Month_Is_Invalid_Returns_Error(int month)
        {
            // Arrange
            var request = new CreateExpenseReportRequest(Guid.NewGuid(), 2026, month);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(CreateExpenseReportRequest.Month), error.PropertyName);
            Assert.Equal("expense_report.invalid_period", error.ErrorCode);
        }
    }
}
