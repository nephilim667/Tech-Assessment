using WebApi.Domain.Common;

namespace WebApi.Application.Authentication.Errors
{
    internal static class AuthenticationErrors
    {
        public static readonly Error InvalidCredentials = new(
            "auth.invalid_credentials",
            "Identifiants invalides.");
    }
}
