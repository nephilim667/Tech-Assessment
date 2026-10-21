using WebApi.Domain.Models;

namespace WebApi.UnitTests.Domain.Users
{
    public sealed class UserTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_When_Monthly_Quota_Is_Below_One_Returns_Failure(int maxExpensesPerMonth)
        {
            // Arrange
            const string firstName = "Juste";
            const string lastName = "Leblanc";
            const string street = "10 rue des Lilas";
            const string postalCode = "75001";
            const string city = "Paris";

            // Act
            var result = User.Create(
                firstName,
                lastName,
                street,
                postalCode,
                city,
                maxExpensesPerMonth);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(
                "user.invalid_monthly_quota",
                result.Error.Code);
        }

        [Fact]
        public void Create_When_Monthly_Quota_Is_One_Creates_User()
        {
            // Act
            var result = User.Create(
                firstName: "Juste",
                lastName: "Leblanc",
                street: "10 rue des Lilas",
                postalCode: "75001",
                city: "Paris",
                maxExpensesPerMonth: 1);

            // Assert
            Assert.True(result.IsSuccess);

            var user = result.Value;

            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal("Juste", user.FirstName);
            Assert.Equal("Leblanc", user.LastName);
            Assert.Equal("10 rue des Lilas", user.Street);
            Assert.Equal("75001", user.PostalCode);
            Assert.Equal("Paris", user.City);
            Assert.Equal(1, user.MaxExpensesPerMonth);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_FirstName_Is_Missing_Returns_Failure(string? firstName)
        {
            // Act
            var result = User.Create(
                firstName: firstName!,
                lastName: "Leblanc",
                street: "10 rue des Lilas",
                postalCode: "75001",
                city: "Paris",
                maxExpensesPerMonth: 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_first_name", result.Error.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_LastName_Is_Missing_Returns_Failure(string? lastName)
        {
            // Act
            var result = User.Create(
                firstName: "Juste",
                lastName: lastName!,
                street: "10 rue des Lilas",
                postalCode: "75001",
                city: "Paris",
                maxExpensesPerMonth: 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_last_name", result.Error.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_Street_Is_Missing_Returns_Failure(string? street)
        {
            // Act
            var result = User.Create(
                firstName: "Juste",
                lastName: "Leblanc",
                street: street!,
                postalCode: "75001",
                city: "Paris",
                maxExpensesPerMonth: 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_street", result.Error.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_PostalCode_Is_Missing_Returns_Failure(string? postalCode)
        {
            var result = User.Create(
                firstName: "Juste",
                lastName: "Leblanc",
                street: "10 rue des Lilas",
                postalCode: postalCode!,
                city: "Paris",
                maxExpensesPerMonth: 1);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_postal_code", result.Error.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_When_City_Is_Missing_Returns_Failure(string? city)
        {
            var result = User.Create(
                firstName: "Juste",
                lastName: "Leblanc",
                street: "10 rue des Lilas",
                postalCode: "75001",
                city: city!,
                maxExpensesPerMonth: 1);

            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_city", result.Error.Code);
        }
    }
}
