using WebApi.Application.Interfaces.Users;
using WebApi.Infrastructure.Api.Users.Mappings;

namespace WebApi.Infrastructure.Api.Users.Endpoints
{
    internal static class GetUserByIdEndpoint
    {
        internal static async Task<IResult> GetAsync(Guid id, IUserService service, CancellationToken cancellationToken)
        {
            var result = await service.GetByIdAsync(id, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: result.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.Error.Code
                    });
            }

            return Results.Ok(result.Value.ToResponse());
        }
    }
}
