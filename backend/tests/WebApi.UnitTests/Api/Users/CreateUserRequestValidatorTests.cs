using FluentValidation.TestHelper;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Validators;

namespace WebApi.UnitTests.Api.Users
{
    public sealed class CreateUserRequestValidatorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_When_Quota_Is_Below_One_Returns_Error(
        int maxExpensesPerMonth)
        {
            // Arrange
            var request = new CreateUserRequest(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                maxExpensesPerMonth);

            var validator = new CreateUserRequestValidator();

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(
                    request => request.MaxExpensesPerMonth)
                .WithErrorCode("user.invalid_monthly_quota");
        }

        [Fact]
        public void Validate_When_Request_Is_Valid_Returns_No_Error()
        {
            // Arrange
            var request = new CreateUserRequest(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                1);

            var validator = new CreateUserRequestValidator();

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_When_Required_Fields_Are_Missing_Returns_Field_Errors(string? value)
        {
            // Arrange
            var request = new CreateUserRequest(
                value!,
                value!,
                value!,
                value!,
                value!,
                1);

            var validator = new CreateUserRequestValidator();

            // Act
            var result = validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(request => request.FirstName)
                .WithErrorCode("user.invalid_first_name");

            result.ShouldHaveValidationErrorFor(request => request.LastName)
                .WithErrorCode("user.invalid_last_name");

            result.ShouldHaveValidationErrorFor(request => request.Street)
                .WithErrorCode("user.invalid_street");

            result.ShouldHaveValidationErrorFor(request => request.PostalCode)
                .WithErrorCode("user.invalid_postal_code");

            result.ShouldHaveValidationErrorFor(request => request.City)
                .WithErrorCode("user.invalid_city");

            result.ShouldNotHaveValidationErrorFor(
                request => request.MaxExpensesPerMonth);
        }
    }
}
