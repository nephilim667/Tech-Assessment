using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Administrators.Errors
{
    internal static class AdministratorAccountRequestErrors
    {
        public static readonly Error InvalidEmail = new(
            "admin.invalid_email",
            "Une adresse email valide est obligatoire.");

        public static readonly Error InvalidPassword = new(
            "admin.invalid_password",
            "Le mot de passe doit contenir au moins 12 caractères, "
            + "une majuscule, une minuscule, un chiffre et un caractère spécial.");
    }
}
