using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.Authentication.Contracts;
using WebApi.Infrastructure.Api.Authentication.Endpoints;

namespace WebApi.Infrastructure.Api.Authentication
{
    internal static class AuthenticationEndpoints
    {
        public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var authentication = endpoints.MapGroup("/api/auth").WithTags("Authentication");

            authentication.MapPost("/login", AuthenticateEndpoint.AuthenticateAsync)
                .AddEndpointFilter<ValidationFilter<AuthenticationRequest>>()
                .AllowAnonymous()
                .Produces<AuthenticationResponse>()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesValidationProblem();

            return endpoints;
        }
    }
}
