using NSubstitute;

using WebApi.Domain.Common;
using WebApi.Domain.Models;

using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Users;

namespace WebApi.UnitTests.Application.Users
{
    public sealed class UserStatusServiceTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        public UserStatusServiceTests()
        {
            // Exécute le cas d’usage passé à l’abstraction transactionnelle.
            // La transaction réelle est vérifiée par les tests d’intégration.
            _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Result<User>>>>(), Arg.Any<CancellationToken>())
                       .Returns(call => call.ArgAt<Func<CancellationToken, Task<Result<User>>>>(0)(
                                call.ArgAt<CancellationToken>(1)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetActiveAsync_When_User_Exists_Persists_Requested_State(bool isActive)
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var user = User.Restore(
                Guid.NewGuid(),
                new UserDetails(
                    "Juste",
                    "Leblanc",
                    "10 rue des Lilas",
                    "75001",
                    "Paris",
                    5),
                isActive: !isActive,
                isDeleted: false);

            _userRepository.GetByIdForUpdateAsync(user.Id, cancellationToken)
                .Returns(Task.FromResult<User?>(user));

            var service = new UserService(_userRepository, _unitOfWork);

            // Act
            var result = await service.SetActiveAsync(user.Id, isActive, cancellationToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(isActive, result.Value.IsActive);

            await _userRepository.Received(1).UpdateStateAsync(user, cancellationToken);
            await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        }

        [Fact]
        public async Task SetActiveAsync_When_User_Is_Missing_Does_Not_Write()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var id = Guid.NewGuid();

            _userRepository.GetByIdForUpdateAsync(id, cancellationToken)
                           .Returns(Task.FromResult<User?>(null));

            var service = new UserService(_userRepository, _unitOfWork);

            // Act
            var result = await service.SetActiveAsync(id, false, cancellationToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("user.not_found", result.Error.Code);

            await _userRepository.DidNotReceive().UpdateStateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
