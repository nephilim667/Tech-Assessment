using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApi.Application.Interfaces.Authentication;

namespace WebApi.Infrastructure.Identity
{
    internal sealed class RevocationJwtBearerEvents(ITokenRevocationStore revocationStore) : JwtBearerEvents
    {
        public override async Task TokenValidated(TokenValidatedContext context)
        {
            var tokenId = context.Principal?
                .FindFirst(JwtRegisteredClaimNames.Jti)?
                .Value;

            if (!Guid.TryParse(tokenId, out _))
            {
                context.Fail("Identifiant de token invalide.");
                return;
            }

            if (await revocationStore.IsRevokedAsync(tokenId!, context.HttpContext.RequestAborted))
            {
                context.Fail("Token révoqué.");
            }
        }
    }
}
