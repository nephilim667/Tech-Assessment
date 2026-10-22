using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

using WebApi.Infrastructure.Api;
using WebApi.Infrastructure.Api.Users;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisClient(connectionName: "cache");

builder.AddNpgsqlDbContext<AppDbContext>(connectionName: "database",
    configureDbContextOptions: options =>
        options.UseAsyncSeeding(async (context, _, cancellationToken) =>
            await AppDbContext.SeedAsync(context, cancellationToken)));

builder.Services
    .AddOpenApi()
    .AddPersistence()
    .AddApplication()
    .AddValidation()
    .AddIdentityServices();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var sp = app.Services.CreateScope();
        await sp.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await sp.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedSuperAdminAsync();
    }
    catch (Exception e)
    {
        app.Services.GetRequiredService<ILogger<Program>>()
            .LogError(e, "An error occurred while initializing the database and Identity.");
        return;
    }
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("N2JSoft — API de gestion des dépenses"));
}

app.UseHttpsRedirection();

app.MapUserEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class Program;