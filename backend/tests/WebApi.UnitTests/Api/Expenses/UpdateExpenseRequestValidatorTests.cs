using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Validators;

namespace WebApi.UnitTests.Api.Expenses
{
    public sealed class UpdateExpenseRequestValidatorTests
    {
        private readonly UpdateExpenseRequestValidator _validator = new();

        [Fact]
        public void Validate_When_Request_Is_Valid_Succeeds()
        {
            var result = _validator.Validate(CreateRequest());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_When_Destination_Is_Missing_Returns_Error()
        {
            var request = CreateRequest() with
            {
                DestinationExpenseReportId = Guid.Empty
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(UpdateExpenseRequest.DestinationExpenseReportId), error.PropertyName);
            Assert.Equal("expense.destination_required", error.ErrorCode);
        }

        [Fact]
        public void Validate_When_Date_Is_Missing_Returns_Error()
        {
            var request = CreateRequest() with { Date = null };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Equal("expense.date_required", Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Validate_When_Amount_Has_Too_Many_Decimals_Returns_Error()
        {
            var request = CreateRequest() with { Amount = 12.345m };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Equal("expense.invalid_amount", Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Validate_When_Description_Is_Too_Long_Returns_Error()
        {
            var request = CreateRequest() with
            {
                Description = new string('a', 51)
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Equal("expense.invalid_description", Assert.Single(result.Errors).ErrorCode);
        }

        // Helpers
        private static UpdateExpenseRequest CreateRequest() =>
            new()
            {
                DestinationExpenseReportId = Guid.NewGuid(),
                Date = new DateOnly(2026, 10, 15),
                Description = "Déjeuner",
                Amount = 25.50m,
                BillingStreet = "20 rue du Restaurant",
                BillingPostalCode = "69001",
                BillingCity = "Lyon"
            };
    }
}
