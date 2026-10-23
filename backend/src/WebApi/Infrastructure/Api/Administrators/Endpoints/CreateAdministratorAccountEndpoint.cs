using WebApi.Application.Authentication.Errors;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Infrastructure.Api.Administrators.Contracts;

namespace WebApi.Infrastructure.Api.Administrators.Endpoints
{
    internal static class CreateAdministratorAccountEndpoint
    {
        internal static async Task<IResult> CreateAsync(CreateAdministratorAccountRequest request, IAdministratorAccountProvider provider, CancellationToken cancellationToken)
        {
            var result = await provider.CreateAsync(request.Email, request.Password, cancellationToken);

            if (!result.IsSuccess)
            {
                var statusCode =
                    result.Error.Code == AdministratorAccountErrors.EmailAlreadyUsed.Code
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest;

                return Results.Problem(
                    statusCode: statusCode,
                    title: result.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.Error.Code
                    });
            }

            return Results.Json(
                new AdministratorAccountResponse(
                    result.Value.Id,
                    result.Value.Email),
                statusCode: StatusCodes.Status201Created);
        }
    }
}
