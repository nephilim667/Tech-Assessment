using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence.Entities;
using WebApi.Infrastructure.Persistence.Mappings;

namespace WebApi.UnitTests.Infrastructure.Persistence;

public sealed class UserMappingTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ToDomain_Preserves_Persisted_Data(bool isActive, bool isDeleted)
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = Guid.NewGuid(),
            FirstName = "Juste",
            LastName = "Leblanc",
            Street = "10 rue des Lilas",
            PostalCode = "75001",
            City = "Paris",
            MaxExpensesPerMonth = 5,
            IsActive = isActive,
            IsDeleted = isDeleted
        };

        // Act
        var user = entity.ToDomain();

        // Assert
        Assert.Equal(entity.Id, user.Id);
        Assert.Equal(entity.FirstName, user.FirstName);
        Assert.Equal(entity.LastName, user.LastName);
        Assert.Equal(entity.Street, user.Street);
        Assert.Equal(entity.PostalCode, user.PostalCode);
        Assert.Equal(entity.City, user.City);
        Assert.Equal(entity.MaxExpensesPerMonth, user.MaxExpensesPerMonth);
        Assert.Equal(isActive, user.IsActive);
        Assert.Equal(isDeleted, user.IsDeleted);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ToEntity_Preserves_Domain_Data(bool isActive, bool isDeleted)
    {
        // Arrange
        var user = User.Restore(
            id: Guid.NewGuid(),
            details: new UserDetails(
                FirstName: "Juste",
                LastName: "Leblanc",
                Street: "10 rue des Lilas",
                PostalCode: "75001",
                City: "Paris",
                MaxExpensesPerMonth: 5),
            isActive: isActive,
            isDeleted: isDeleted);

        // Act
        var entity = user.ToEntity();

        // Assert
        Assert.Equal(user.Id, entity.Id);
        Assert.Equal(user.FirstName, entity.FirstName);
        Assert.Equal(user.LastName, entity.LastName);
        Assert.Equal(user.Street, entity.Street);
        Assert.Equal(user.PostalCode, entity.PostalCode);
        Assert.Equal(user.City, entity.City);
        Assert.Equal(user.MaxExpensesPerMonth, entity.MaxExpensesPerMonth);
        Assert.Equal(isActive, entity.IsActive);
        Assert.Equal(isDeleted, entity.IsDeleted);
    }
}