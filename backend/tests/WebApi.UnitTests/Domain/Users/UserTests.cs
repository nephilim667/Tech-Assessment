using WebApi.Domain.Entities;

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
        public void Create_WhenMonthlyQuotaIsOne_CreatesUser()
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
            Assert.Equal(1, result.Value.MaxExpensesPerMonth);
        }
    }
}
