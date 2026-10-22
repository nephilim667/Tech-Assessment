using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var jwtSigningKey = builder.AddParameter("jwt-signing-key",
    new GenerateParameterDefault
    {
        MinLength = 64,
        Lower = true,
        Upper = true,
        Numeric = true,
        Special = false
    },
    secret: true,
    persist: true);

var bootstrapPassword = builder.AddParameter("bootstrap-admin-password",
    new GenerateParameterDefault
    {
        MinLength = 24,
        MinLower = 1,
        MinUpper = 1,
        MinNumeric = 1,
        MinSpecial = 1
    },
    secret: true,
    persist: true);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var mainDatabase = postgres.AddDatabase("database");

var cache = builder.AddRedis("cache").WithDataVolume()
                   .WithArgs("--appendonly", "yes",
                             "--appendfsync", "always",
                             "--maxmemory-policy", "noeviction");

var webapi = builder.AddProject<Projects.WebApi>("webapi")
    .WithReference(cache)
    .WithReference(mainDatabase)
    .WaitFor(postgres)
    .WaitFor(mainDatabase)
    .WaitFor(cache)
    .WithEnvironment("Jwt__Issuer", "N2JSoft.WebApi")
    .WithEnvironment("Jwt__Audience", "N2JSoft.Frontend")
    .WithEnvironment("Jwt__ExpirationMinutes", "60")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("BootstrapAdmin__Email", "superadmin@n2jsoft.local")
    .WithEnvironment("BootstrapAdmin__Password", bootstrapPassword);

var angular = builder.AddNpmApp("angular", "../../../frontend/")
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();


builder.Build().Run();