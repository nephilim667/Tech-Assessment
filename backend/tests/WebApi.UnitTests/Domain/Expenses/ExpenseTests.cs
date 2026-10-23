using WebApi.Domain.Contracts.Expenses;
using WebApi.Domain.Models;

namespace WebApi.UnitTests.Domain.Expenses
{
    public sealed class ExpenseTests
    {
        [Fact]
        public void Create_When_Data_Is_Valid_Creates_Expense()
        {
            // Arrange
            var report = CreateReport();
            var details = CreateDetails();

            // Act
            var result = Expense.Create(report, details);

            // Assert
            Assert.True(result.IsSuccess);

            var expense = result.Value;

            Assert.NotEqual(Guid.Empty, expense.Id);
            Assert.Equal(report.Id, expense.ExpenseReportId);
            Assert.Equal(details.Date, expense.Date);
            Assert.Equal(details.Description, expense.Description);
            Assert.Equal(details.Amount, expense.Amount);
            Assert.Equal(details.BillingStreet, expense.BillingStreet);
            Assert.Equal(details.BillingPostalCode, expense.BillingPostalCode);
            Assert.Equal(details.BillingCity, expense.BillingCity);
            Assert.Equal(details.BillingBrand, expense.BillingBrand);
            Assert.False(expense.IsDeleted);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_Description_Is_Missing_Returns_Failure(string? description)
        {
            // Arrange
            var details = CreateDetails() with { Description = description! };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_description", result.Error.Code);
        }

        [Fact]
        public void Create_When_Description_Exceeds_50_Characters_Returns_Failure()
        {
            // Arrange
            var details = CreateDetails() with
            {
                Description = new string('a', 51)
            };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_description", result.Error.Code);
        }

        [Fact]
        public void Create_When_Description_Has_50_Characters_Succeeds()
        {
            // Arrange
            var details = CreateDetails() with
            {
                Description = new string('a', 50)
            };

            // Act
            var result = Expense.Create(CreateReport(), details);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_When_Amount_Is_Not_Positive_Returns_Failure(int amount)
        {
            // Arrange
            var details = CreateDetails() with { Amount = amount };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_amount", result.Error.Code);
        }

        [Fact]
        public void Create_When_Amount_Has_More_Than_Two_Decimals_Returns_Failure()
        {
            // Arrange
            var details = CreateDetails() with { Amount = 12.345m };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.invalid_amount", result.Error.Code);
        }

        [Theory]
        [InlineData(2026, 9, 30)]
        [InlineData(2025, 10, 15)]
        public void Create_When_Date_Is_Outside_Report_Period_Returns_Failure(int year,int month,int day)
        {
            // Arrange
            var details = CreateDetails() with
            {
                Date = new DateOnly(year, month, day)
            };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("expense.date_outside_report_period", result.Error.Code);
        }

        [Theory]
        [InlineData("Street", "expense.billing_street_required")]
        [InlineData("PostalCode", "expense.billing_postal_code_required")]
        [InlineData("City", "expense.billing_city_required")]
        public void Create_When_Billing_Address_Is_Incomplete_Returns_Failure(string field,string expectedCode)
        {
            // Arrange
            var details = CreateDetails();

            details = field switch
            {
                "Street" => details with { BillingStreet = " " },
                "PostalCode" => details with { BillingPostalCode = " " },
                "City" => details with { BillingCity = " " },
                _ => throw new ArgumentOutOfRangeException(nameof(field))
            };

            // Act
            var result = Expense.Create(CreateReport(), details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedCode, result.Error.Code);
        }


        // Helpers
        private static ExpenseReport CreateReport()
        {
            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            return ExpenseReport.Create(user, 2026, 10).Value;
        }

        private static ExpenseDetails CreateDetails() =>
            new(
                Date: new DateOnly(2026, 10, 15),
                Description: "Déjeuner déplacement",
                Amount: 25.50m,
                BillingStreet: "20 rue du Restaurant",
                BillingPostalCode: "69001",
                BillingCity: "Lyon",
                BillingBrand: "Restaurant du Centre");
    }
}
