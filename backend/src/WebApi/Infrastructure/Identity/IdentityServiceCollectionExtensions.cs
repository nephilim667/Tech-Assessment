using System.Text;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;

using WebApi.Application.Common;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Infrastructure.Identity.Configuration;

namespace WebApi.Infrastructure.Identity
{
    internal static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddTokenIssuance(
        this IServiceCollection services)
        {
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer est obligatoire.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience est obligatoire.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.SigningKey)
                        && Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                    "Jwt:SigningKey doit contenir au moins 32 octets.")
                .Validate(options => options.ExpirationMinutes > 0, "La durée de validité du JWT doit être positive.")
                .ValidateOnStart();

            services.TryAddSingleton<TimeProvider>(TimeProvider.System);

            services.AddScoped<IAccessTokenIssuer, JwtTokenIssuer>();

            return services;
        }

        public static IServiceCollection AddBearerAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, configuredJwt) =>
                {
                    var jwt = configuredJwt.Value;

                    bearer.MapInboundClaims = false;

                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.SigningKey)),
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = "role",
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    AuthorizationPolicies.Administrators,
                    policy => policy
                        .RequireAuthenticatedUser()
                        .RequireRole(SecurityRoles.Admin, SecurityRoles.SuperAdmin));
            });

            return services;
        }
    }
}
