using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Endpoints;

namespace WebApi.Infrastructure.Api.Users;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users").WithTags("Users").RequireAuthorization(AuthorizationPolicies.Administrators);

        users.MapGet("/{id:guid}", GetUserByIdEndpoint.GetAsync);
        users.MapGet("", GetUsersEndpoint.GetAsync);
        users.MapPost("", CreateUserEndpoint.CreateAsync).AddEndpointFilter<ValidationFilter<CreateUserRequest>>();
        users.MapPut("/{id:guid}/status", SetUserStatusEndpoint.SetAsync).AddEndpointFilter<ValidationFilter<SetUserStatusRequest>>();
        users.MapDelete("/{id:guid}", DeleteUserEndpoint.DeleteAsync);

        return endpoints;
    }
}