namespace WebApi.Infrastructure.Identity.Configuration
{
    internal sealed class BootstrapAdminOptions
    {
        public const string SectionName = "BootstrapAdmin";

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

    }
}
