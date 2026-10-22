using FluentValidation;
using Microsoft.AspNetCore.Identity;

using WebApi.Application.Users;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;

using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Persistence.Common;
using WebApi.Infrastructure.Persistence.Repositories;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Validators;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Identity.Configuration;

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

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<SetUserStatusRequest>, SetUserStatusRequestValidator>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<IdentityUser<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IdentitySeeder>();
        services.AddOptions<BootstrapAdminOptions>().BindConfiguration(BootstrapAdminOptions.SectionName);

        return services;
    }
}