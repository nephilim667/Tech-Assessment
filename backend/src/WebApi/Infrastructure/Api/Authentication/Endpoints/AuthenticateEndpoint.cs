using WebApi.Application.Interfaces.Authentication;
using WebApi.Infrastructure.Api.Authentication.Contracts;

namespace WebApi.Infrastructure.Api.Authentication.Endpoints
{
    internal static class AuthenticateEndpoint
    {
        internal static async Task<IResult> AuthenticateAsync(AuthenticationRequest request, IAuthenticationProvider provider, CancellationToken cancellationToken)
        {
            var result = await provider.AuthenticateAsync(request.Email, request.Password, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: result.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.Error.Code
                    });
            }

            return Results.Ok(new AuthenticationResponse(result.Value.AccessToken, "Bearer", result.Value.ExpiresAtUtc));
        }
    }
}
