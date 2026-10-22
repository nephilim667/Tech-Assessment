using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Authentication.Errors
{
    internal static class AuthenticationRequestErrors
    {
        public static readonly Error EmailRequired = new(
            "auth.email_required",
            "L’adresse email est obligatoire.");

        public static readonly Error PasswordRequired = new(
            "auth.password_required",
            "Le mot de passe est obligatoire.");
    }
}
