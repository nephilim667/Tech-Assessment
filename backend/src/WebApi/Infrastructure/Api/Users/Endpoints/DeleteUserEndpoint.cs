using WebApi.Application.Interfaces.Users;
using WebApi.Domain.Errors;

namespace WebApi.Infrastructure.Api.Users.Endpoints
{
    internal static class DeleteUserEndpoint
    {
        internal static async Task<IResult> DeleteAsync(Guid id, IUserService service, CancellationToken cancellationToken)
        {
            var result = await service.DeleteAsync(id, cancellationToken);

            if (!result.IsSuccess)
            {
                var statusCode = result.Error.Code == UserErrors.NotFound.Code
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status409Conflict;

                return Results.Problem(
                    statusCode: statusCode,
                    title: result.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.Error.Code
                    });
            }

            return Results.NoContent();
        }
    }
}
