using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Expenses.Errors
{
    internal static class ExpenseRequestErrors
    {
        public static readonly Error DateRequired =
            new("expense.date_required", "La date de la dépense est obligatoire.");
    }
}
