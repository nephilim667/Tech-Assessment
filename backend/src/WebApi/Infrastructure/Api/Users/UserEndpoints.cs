using FluentValidation;

using WebApi.Domain.Models;
using WebApi.Application.Interfaces.Users;

using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Api.Common;
using WebApi.Infrastructure.Api.Users.Contracts;

namespace WebApi.Infrastructure.Api.Users;

internal static class UserEndpoints
{
    public sealed record UserInfo(Guid Id, string Name);

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users").WithTags("Users");

        users.MapGet("", (AppDbContext db) => Results.Ok(db.Users.Select(user => new UserInfo(user.Id, $"{user.FirstName} {user.LastName}"))));
        users.MapPost("", CreateUserEndpoint.CreateAsync).AddEndpointFilter<ValidationFilter<CreateUserRequest>>();

        return endpoints;
    }
}