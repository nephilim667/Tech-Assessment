using System.Text;
using System.Globalization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Application.Authentication.Tokens;
using WebApi.Application.Interfaces.Authentication;
using WebApi.Infrastructure.Identity.Configuration;

namespace WebApi.Infrastructure.Identity
{
    internal sealed class JwtTokenIssuer(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider) : IAccessTokenIssuer
    {
        public IssuedAccessToken Generate(Guid accountId, IReadOnlyCollection<string> roles)
        {
            var options = jwtOptions.Value;
            var now = timeProvider.GetUtcNow();
            var expiration = now.AddMinutes(options.ExpirationMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
            };

            claims.AddRange(roles.Distinct().Select(role => new Claim("role", role)));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

            var token = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiration.UtcDateTime,
                signingCredentials: new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256));

            return new IssuedAccessToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiration);
        }
    }
}
