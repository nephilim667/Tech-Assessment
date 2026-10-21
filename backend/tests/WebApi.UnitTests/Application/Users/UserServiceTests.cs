using NSubstitute;

using WebApi.Domain.Models;
using WebApi.Application.Users;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Interfaces.Common;

namespace WebApi.UnitTests.Application.Users
{
    public sealed class UserServiceTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        [Fact]
        public async Task CreateAsync_When_Data_Is_Valid_Persists_User()
        {
            // Arrange
            var details = new UserDetails(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5);

            var service = new UserService(_userRepository, _unitOfWork);

            // Act
            var result = await service.CreateAsync(details);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Juste", result.Value.FirstName);
            Assert.Equal(5, result.Value.MaxExpensesPerMonth);

            await _userRepository.Received(1).AddAsync(
                result.Value,
                Arg.Any<CancellationToken>());

            await _unitOfWork.Received(1).SaveChangesAsync(
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateAsync_When_Quota_Is_Invalid_Does_Not_Persist(
            int maxExpensesPerMonth)
        {
            // Arrange
            var details = new UserDetails(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                maxExpensesPerMonth);

            var service = new UserService(_userRepository, _unitOfWork);

            // Act
            var result = await service.CreateAsync(details);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.invalid_monthly_quota", result.Error.Code);

            await _userRepository.DidNotReceive().AddAsync(
                Arg.Any<User>(),
                Arg.Any<CancellationToken>());

            await _unitOfWork.DidNotReceive().SaveChangesAsync(
                Arg.Any<CancellationToken>());
        }
    }
}
