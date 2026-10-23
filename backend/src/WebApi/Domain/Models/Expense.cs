using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Expenses;
using WebApi.Domain.Errors;

namespace WebApi.Domain.Models
{
    internal sealed class Expense
    {
        private Expense(Guid id, Guid expenseReportId,ExpenseDetails details,bool isDeleted)
        {
            Id = id;
            ExpenseReportId = expenseReportId;
            Date = details.Date;
            Description = details.Description;
            Amount = details.Amount;
            BillingStreet = details.BillingStreet;
            BillingPostalCode = details.BillingPostalCode;
            BillingCity = details.BillingCity;
            BillingBrand = details.BillingBrand;
            IsDeleted = isDeleted;
        }

        public Guid Id { get; }
        public Guid ExpenseReportId { get; }
        public DateOnly Date { get; }
        public string Description { get; }
        public decimal Amount { get; }
        public string BillingStreet { get; }
        public string BillingPostalCode { get; }
        public string BillingCity { get; }
        public string? BillingBrand { get; }
        public bool IsDeleted { get; }

        public static Result<Expense> Create(ExpenseReport report, ExpenseDetails details)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentNullException.ThrowIfNull(details);

            if (string.IsNullOrWhiteSpace(details.Description) || details.Description.Length > 50)
                return Result<Expense>.Failure(ExpenseErrors.InvalidDescription);

            if (details.Amount <= 0 || decimal.Round(details.Amount, 2) != details.Amount)
                return Result<Expense>.Failure(ExpenseErrors.InvalidAmount);

            if (details.Date.Year != report.Year || details.Date.Month != report.Month)
                return Result<Expense>.Failure(ExpenseErrors.DateOutsideReportPeriod);

            if (string.IsNullOrWhiteSpace(details.BillingStreet))
                return Result<Expense>.Failure(ExpenseErrors.BillingStreetRequired);

            if (string.IsNullOrWhiteSpace(details.BillingPostalCode))
                return Result<Expense>.Failure(ExpenseErrors.BillingPostalCodeRequired);

            if (string.IsNullOrWhiteSpace(details.BillingCity))
                return Result<Expense>.Failure(ExpenseErrors.BillingCityRequired);

            return Result<Expense>.Success(
                new Expense(
                    Guid.NewGuid(),
                    report.Id,
                    details,
                    isDeleted: false));
        }

        // Restaure l’état persisté sans réappliquer les règles de création.
        internal static Expense Restore(Guid id, Guid expenseReportId, ExpenseDetails details, bool isDeleted) => new(id, expenseReportId, details, isDeleted);
    }
}
