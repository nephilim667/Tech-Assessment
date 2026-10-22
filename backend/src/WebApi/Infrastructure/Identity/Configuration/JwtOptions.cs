namespace WebApi.Infrastructure.Identity.Configuration
{
    internal sealed class JwtOptions
    {
        public const string SectionName = "Jwt";
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 60;
    }
}
