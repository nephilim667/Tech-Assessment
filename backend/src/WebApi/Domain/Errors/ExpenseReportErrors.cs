using WebApi.Domain.Common;

namespace WebApi.Domain.Errors
{
    internal static class ExpenseReportErrors
    {
        public static readonly Error InvalidPeriod =
            new("expense_report.invalid_period", "La période doit contenir une année entre 1 et 9999 et un mois entre 1 et 12.");
    }
}
