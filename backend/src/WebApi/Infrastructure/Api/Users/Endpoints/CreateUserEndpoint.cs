using WebApi.Domain.Models;

using WebApi.Application.Interfaces.Users;
using WebApi.Infrastructure.Api.Users.Mappings;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.Infrastructure.Api.Users.Endpoints
{
    internal static class CreateUserEndpoint
    {
        internal static async Task<IResult> CreateAsync(CreateUserRequest request, IUserService service, CancellationToken cancellationToken)
        {
            var result = await service.CreateAsync(
                new UserDetails(
                    request.FirstName,
                    request.LastName,
                    request.Street,
                    request.PostalCode,
                    request.City,
                    request.MaxExpensesPerMonth),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: result.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.Error.Code
                    });
            }

            var user = result.Value;
            return Results.Created($"/api/users/{user.Id}", user.ToResponse());
        }
    }
}
