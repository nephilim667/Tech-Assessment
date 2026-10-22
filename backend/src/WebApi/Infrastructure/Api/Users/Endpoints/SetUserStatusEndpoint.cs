using WebApi.Application.Interfaces.Users;
using WebApi.Domain.Errors;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Mappings;

namespace WebApi.Infrastructure.Api.Users.Endpoints
{
    internal static class SetUserStatusEndpoint
    {
        internal static async Task<IResult> SetAsync(Guid id, SetUserStatusRequest request, IUserService service, CancellationToken cancellationToken)
        {
            var result = await service.SetActiveAsync(id, request.IsActive!.Value, cancellationToken);

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

            return Results.Ok(result.Value.ToResponse());
        }
    }
}
