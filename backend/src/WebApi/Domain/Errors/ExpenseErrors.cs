using WebApi.Domain.Common;

namespace WebApi.Domain.Errors
{
    internal static class ExpenseErrors
    {
        public static readonly Error InvalidDescription =
        new("expense.invalid_description", "La description est obligatoire et limitée à 50 caractères.");

        public static readonly Error InvalidAmount =
            new("expense.invalid_amount", "Le montant doit être strictement positif et comporter au maximum deux décimales.");

        public static readonly Error DateOutsideReportPeriod =
            new("expense.date_outside_report_period", "La date de la dépense doit appartenir au mois et à l’année de la note de frais.");

        public static readonly Error BillingStreetRequired =
            new("expense.billing_street_required", "La rue de facturation est obligatoire.");

        public static readonly Error BillingPostalCodeRequired =
            new("expense.billing_postal_code_required", "Le code postal de facturation est obligatoire.");

        public static readonly Error BillingCityRequired =
            new("expense.billing_city_required", "La ville de facturation est obligatoire.");

        public static readonly Error MonthlyQuotaReached =
            new("expense.monthly_quota_reached", "Le nombre maximum de dépenses autorisées pour ce mois est atteint.");

        public static readonly Error Deleted =
            new("expense.deleted", "Une dépense supprimée ne peut plus être modifiée.");

        public static readonly Error NotFound =
            new("expense.not_found", "La dépense demandée est introuvable.");

        public static readonly Error DestinationUserMismatch =
            new("expense.destination_user_mismatch", "La note de destination doit appartenir au même utilisateur.");
    }
}
