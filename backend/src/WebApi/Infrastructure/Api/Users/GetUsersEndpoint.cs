using WebApi.Application.Interfaces.Users;
using WebApi.Infrastructure.Api.Users.Mappings;

namespace WebApi.Infrastructure.Api.Users
{
    internal static class GetUsersEndpoint
    {
        internal static async Task<IResult> GetAsync(IUserService service, CancellationToken cancellationToken)
        {
            var users = await service.GetAllAsync(cancellationToken);

            return Results.Ok(users.Select(user => user.ToResponse()).ToArray());
        }
    }
}
