using WebApi.Domain.Models;
using WebApi.Infrastructure.Persistence.Entities;

namespace WebApi.Infrastructure.Persistence.Mappings
{
    internal static class UserMapping
    {
        public static User ToDomain(this UserEntity entity)
        {
            return User.Restore(
                id: entity.Id,
                details: new UserDetails(
                    FirstName: entity.FirstName,
                    LastName: entity.LastName,
                    Street: entity.Street,
                    PostalCode: entity.PostalCode,
                    City: entity.City,
                    MaxExpensesPerMonth: entity.MaxExpensesPerMonth),
                isActive: entity.IsActive,
                isDeleted: entity.IsDeleted);
        }

        public static UserEntity ToEntity(this User user)
        {
            return new UserEntity
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Street = user.Street,
                PostalCode = user.PostalCode,
                City = user.City,
                MaxExpensesPerMonth = user.MaxExpensesPerMonth,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted
            };
        }
    }
}
