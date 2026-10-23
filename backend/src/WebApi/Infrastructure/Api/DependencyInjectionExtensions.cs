using FluentValidation;
using Microsoft.AspNetCore.Identity;

using WebApi.Application.Users;
using WebApi.Application.Expenses;
using WebApi.Application.ExpenseReports;

using WebApi.Application.Interfaces.Common;
using WebApi.Application.Interfaces.Users;
using WebApi.Application.Interfaces.Expenses;
using WebApi.Application.Interfaces.ExpenseReports;
using WebApi.Application.Interfaces.Authentication;

using WebApi.Infrastructure.Api.Common.ExceptionHandler;

using WebApi.Infrastructure.Api.Users.Contracts;
using WebApi.Infrastructure.Api.Users.Validators;
using WebApi.Infrastructure.Api.Administrators.Contracts;
using WebApi.Infrastructure.Api.Administrators.Validators;
using WebApi.Infrastructure.Api.Authentication.Contracts;
using WebApi.Infrastructure.Api.Authentication.Validators;
using WebApi.Infrastructure.Api.Expenses.Contracts;
using WebApi.Infrastructure.Api.Expenses.Validators;
using WebApi.Infrastructure.Api.ExpenseReports.Contracts;
using WebApi.Infrastructure.Api.ExpenseReports.Validators;

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
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IExpenseReportRepository, ExpenseReportRepository>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IExpenseReportService, ExpenseReportService>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<SetUserStatusRequest>, SetUserStatusRequestValidator>();
        services.AddScoped<IValidator<AuthenticationRequest>, AuthenticationRequestValidator>();
        services.AddScoped<IValidator<CreateAdministratorAccountRequest>, CreateAdministratorAccountRequestValidator>();
        services.AddScoped<IValidator<CreateExpenseRequest>, CreateExpenseRequestValidator>();
        services.AddScoped<IValidator<CreateExpenseReportRequest>, CreateExpenseReportRequestValidator>();
        services.AddScoped<IValidator<GetExpensesRequest>, GetExpensesRequestValidator>();
        services.AddScoped<IValidator<UpdateExpenseRequest>, UpdateExpenseRequestValidator>();

        return services;
    }

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<RedisExceptionHandler>();

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
        services.AddScoped<IAdministratorAccountProvider, IdentityAdministratorAccountProvider>();
        services.AddOptions<BootstrapAdminOptions>().BindConfiguration(BootstrapAdminOptions.SectionName);

        services.AddScoped<IAuthenticationProvider, IdentityAuthenticationProvider>();

        return services;
    }
}