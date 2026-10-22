using System.Globalization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using WebApi.Application.Interfaces.Authentication;

namespace WebApi.Infrastructure.Api.Authentication.Endpoints
{
    internal static class LogoutEndpoint
    {
        internal static async Task<IResult> LogoutAsync(ClaimsPrincipal user, ITokenRevocationStore revocationStore, CancellationToken cancellationToken)
        {
            var tokenId = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expiration = user.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (!Guid.TryParse(tokenId, out _) || !long.TryParse(expiration, NumberStyles.None, CultureInfo.InvariantCulture, out var expirationTimestamp))
                return Results.Unauthorized();

            await revocationStore.RevokeAsync(tokenId!, DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp), cancellationToken);

            return Results.NoContent();
        }
    }
}
