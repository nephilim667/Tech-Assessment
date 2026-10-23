using System.Globalization;

using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Validators;

namespace WebApi.UnitTests.Api.Expenses
{
    public sealed class CreateExpenseRequestValidatorTests
    {
        private readonly CreateExpenseRequestValidator _validator = new();

        [Fact]
        public void Validate_When_Request_Is_Valid_And_Brand_Is_Absent_Succeeds()
        {
            var result = _validator.Validate(CreateRequest());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_When_Date_Is_Missing_Returns_Error()
        {
            var request = CreateRequest() with { Date = null };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(CreateExpenseRequest.Date), error.PropertyName);
            Assert.Equal("expense.date_required", error.ErrorCode);
        }

        [Theory]
        [InlineData("Description", "expense.invalid_description")]
        [InlineData("BillingStreet", "expense.billing_street_required")]
        [InlineData("BillingPostalCode", "expense.billing_postal_code_required")]
        [InlineData("BillingCity", "expense.billing_city_required")]
        public void Validate_When_Required_Text_Is_Blank_Returns_Error(string field, string expectedCode)
        {
            var request = CreateRequest();

            request = field switch
            {
                "Description" => request with { Description = " " },
                "BillingStreet" => request with { BillingStreet = " " },
                "BillingPostalCode" => request with { BillingPostalCode = " " },
                "BillingCity" => request with { BillingCity = " " },
                _ => throw new ArgumentOutOfRangeException(nameof(field))
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(field, error.PropertyName);
            Assert.Equal(expectedCode, error.ErrorCode);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("12.345")]
        public void Validate_When_Amount_Is_Invalid_Returns_Error(string amount)
        {
            var request = CreateRequest() with
            {
                Amount = decimal.Parse(amount, CultureInfo.InvariantCulture)
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(CreateExpenseRequest.Amount), error.PropertyName);
            Assert.Equal("expense.invalid_amount", error.ErrorCode);
        }

        [Fact]
        public void Validate_When_Description_Exceeds_50_Characters_Returns_Error()
        {
            var request = CreateRequest() with
            {
                Description = new string('a', 51)
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal("expense.invalid_description", error.ErrorCode);
        }

        [Fact]
        public void Validate_When_Description_Has_50_Characters_Succeeds()
        {
            var request = CreateRequest() with
            {
                Description = new string('a', 50)
            };

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        private static CreateExpenseRequest CreateRequest() =>
            new(
                Date: new DateOnly(2026, 10, 15),
                Description: "Déjeuner",
                Amount: 25.50m,
                BillingStreet: "20 rue du Restaurant",
                BillingPostalCode: "69001",
                BillingCity: "Lyon");
    }
}
