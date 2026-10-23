using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.ExpenseReports.Errors
{
    internal static class ExpenseReportRequestErrors
    {
        public static readonly Error UserRequired =
            new("expense_report.user_required", "L’utilisateur est obligatoire.");
    }
}
