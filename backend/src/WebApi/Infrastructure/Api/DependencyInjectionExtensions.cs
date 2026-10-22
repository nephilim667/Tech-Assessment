using FluentValidation;

using WebApi.Application.Users;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;

using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Validators;
using WebApi.Infrastructure.Persistence.Common;
using WebApi.Infrastructure.Persistence.Repositories;

namespace WebApi.Infrastructure.Api;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }

    public static IServiceCollection AddValidation(
    this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<SetUserStatusRequest>, SetUserStatusRequestValidator>();

        return services;
    }
}