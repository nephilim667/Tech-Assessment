using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Infrastructure.Identity;
using WebApi.Infrastructure.Identity.Configuration;

namespace WebApi.UnitTests.Infrastructure.Identity
{
    public sealed class JwtTokenIssuerTests
    {
        [Theory]
        [InlineData("Admin")]
        [InlineData("SuperAdmin")]
        public void Generate_Returns_Signed_Token_With_Identity_Role_And_Expiration(string role)
        {
            // Arrange : clé réservée aux tests.
            var options = new JwtOptions
            {
                Issuer = "n2jsoft-tests",
                Audience = "n2jsoft-api-tests",
                SigningKey = new string('K', 64),
                ExpirationMinutes = 60
            };

            var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            var accountId = Guid.NewGuid();

            var issuer = new JwtTokenIssuer(Options.Create(options), new FixedTimeProvider(now));

            // Act
            var result = issuer.Generate(accountId, [role]);

            // Assert : validation cryptographique réelle.
            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

            var principal = handler.ValidateToken(
                result.AccessToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
                },
                out var validatedToken);

            Assert.Equal(accountId.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            Assert.True(principal.IsInRole(role));

            Assert.True(Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value, out _));

            Assert.Equal(now.AddHours(1), result.ExpiresAtUtc);
            Assert.Equal(result.ExpiresAtUtc.UtcDateTime, validatedToken.ValidTo);
        }

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }
    }
}
