using WebApi.Domain.Models;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.Infrastructure.Api.Users.Mappings
{
    internal static class UserResponseMappings
    {
        public static UserResponse ToResponse(this User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Street = user.Street,
                PostalCode = user.PostalCode,
                City = user.City,
                MaxExpensesPerMonth = user.MaxExpensesPerMonth,
                IsActive = user.IsActive
            };
        }
    }
}
