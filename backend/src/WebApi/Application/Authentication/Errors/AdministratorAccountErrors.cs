using WebApi.Domain.Common;

namespace WebApi.Application.Authentication.Errors
{
    internal static class AdministratorAccountErrors
    {
        public static readonly Error EmailAlreadyUsed = new(
            "admin.email_already_used",
            "Cette adresse email est déjà utilisée.");

        public static readonly Error InvalidAccount = new(
            "admin.invalid_account",
            "L’adresse email ou le mot de passe ne respecte pas les règles du compte.");
    }
}
