using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Users;
using WebApi.Infrastructure.Api.Authentication.Contracts;
using WebApi.Infrastructure.Api.Authentication.Validators;
using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Validators;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Identity.Configuration;
using WebApi.Infrastructure.Persistence;
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

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<SetUserStatusRequest>, SetUserStatusRequestValidator>();
        services.AddScoped<IValidator<AuthenticationRequest>, AuthenticationRequestValidator>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<IdentityUser<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager();

        services.AddScoped<IdentitySeeder>();
        services.AddOptions<BootstrapAdminOptions>().BindConfiguration(BootstrapAdminOptions.SectionName);

        services.AddAuthentication();
        services.AddScoped<IAuthenticationProvider, IdentityAuthenticationProvider>();

        return services;
    }
}