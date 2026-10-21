using WebApi.Domain.Common;

namespace WebApi.Domain.Errors
{
    internal static class UserErrors
    {
        public static readonly Error InvalidMonthlyQuota =
            new("user.invalid_monthly_quota", "Le quota mensuel doit être supérieur ou égal à 1.");

        public static readonly Error InvalidFirstName = 
            new("user.invalid_first_name", "Le prénom est obligatoire.");

        public static readonly Error InvalidLastName = 
            new("user.invalid_last_name", "Le nom est obligatoire.");

        public static readonly Error InvalidStreet = 
            new("user.invalid_street", "La rue est obligatoire.");

        public static readonly Error InvalidPostalCode = 
            new("user.invalid_postal_code", "Le code postal est obligatoire.");

        public static readonly Error InvalidCity = 
            new("user.invalid_city", "La ville est obligatoire.");

        public static readonly Error Deleted = 
            new("user.deleted", "Un utilisateur supprimé ne peut plus être modifié.");

    }
}
