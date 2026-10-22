using System.Text;

using Microsoft.Extensions.DependencyInjection.Extensions;

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
    }
}
