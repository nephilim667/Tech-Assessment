using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Common.Errors
{
    internal static class InfrastructureErrors
    {
        public static readonly Error RedisUnavailable = new(
            "infrastructure.redis_unavailable",
            "Le service est temporairement indisponible. Réessayez plus tard.");
    }
}
