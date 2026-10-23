using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Expenses.Errors
{
    internal static class ExpenseRequestErrors
    {
        public static readonly Error DateRequired =
            new("expense.date_required", "La date de la dépense est obligatoire.");

        public static readonly Error DestinationRequired =
            new("expense.destination_required", "La note de frais de destination est obligatoire.");
    }
}
