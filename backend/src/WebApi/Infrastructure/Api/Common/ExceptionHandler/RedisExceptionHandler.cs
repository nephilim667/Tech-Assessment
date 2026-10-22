using Microsoft.AspNetCore.Diagnostics;

using StackExchange.Redis;

using WebApi.Infrastructure.Api.Common.Errors;

namespace WebApi.Infrastructure.Api.Common.ExceptionHandler
{
    internal sealed class RedisExceptionHandler(ILogger<RedisExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not (RedisException or RedisTimeoutException))
                return false;

            logger.LogError(exception, "Une opération Redis nécessaire au traitement a échoué.");

            var error = InfrastructureErrors.RedisUnavailable;

            await Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                })
                .ExecuteAsync(httpContext);

            return true;
        }
    }
}
