using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.Administrators.Contracts;
using WebApi.Infrastructure.Api.Administrators.Endpoints;

namespace WebApi.Infrastructure.Api.Administrators
{
    public static class AdministratorEndpoints
    {
        public static IEndpointRouteBuilder MapAdministratorAccountEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var accounts = endpoints.MapGroup("/api/admin-accounts").WithTags("Administrator accounts").RequireAuthorization(AuthorizationPolicies.SuperAdministrators);

            accounts.MapPost("", CreateAdministratorAccountEndpoint.CreateAsync)
                    .AddEndpointFilter<ValidationFilter<CreateAdministratorAccountRequest>>()
                    .Produces<AdministratorAccountResponse>(StatusCodes.Status201Created)
                    .ProducesValidationProblem()
                    .ProducesProblem(StatusCodes.Status409Conflict)
                    .Produces(StatusCodes.Status401Unauthorized)
                    .Produces(StatusCodes.Status403Forbidden);

            return endpoints;
        }
    }
}
