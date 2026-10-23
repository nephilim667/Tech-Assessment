using System.Globalization;
using WebApi.Domain.Common;
using WebApi.Domain.Errors;

namespace WebApi.Domain.Models
{
    internal sealed class ExpenseReport
    {
        private ExpenseReport(Guid id, Guid userId, string title,int year, int month)
        {
            Id = id;
            UserId = userId;
            Title = title;
            Year = year;
            Month = month;
        }

        public Guid Id { get; }
        public Guid UserId { get; }
        public string Title { get; }
        public int Year { get; }
        public int Month { get; }

        public static Result<ExpenseReport> Create(User user, int year, int month)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.IsDeleted)
                return Result<ExpenseReport>.Failure(UserErrors.Deleted);

            if (!user.IsActive)
                return Result<ExpenseReport>.Failure(UserErrors.Inactive);

            if (year is < 1 or > 9999 || month is < 1 or > 12)
                return Result<ExpenseReport>.Failure(ExpenseReportErrors.InvalidPeriod);

            var period = new DateOnly(year, month, 1);
            var formattedPeriod = period.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));

            var title = $"{user.LastName} {user.FirstName} - {formattedPeriod}";

            return Result<ExpenseReport>.Success(
                new ExpenseReport(Guid.NewGuid(), user.Id, title, year, month));
        }

        // Restaure les données persistées sans recalculer l’intitulé
        // ni appliquer les règles réservées à la création.
        internal static ExpenseReport Restore(Guid id, Guid userId, string title, int year, int month) => new(id, userId, title, year, month);
    }
}
