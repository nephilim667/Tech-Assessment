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
        public Guid ExpenseReportId { get; private set; }
        public DateOnly Date { get; private set; }
        public string Description { get; private set; }
        public decimal Amount { get; private set; }
        public string BillingStreet { get; private set; }
        public string BillingPostalCode { get; private set; }
        public string BillingCity { get; private set; }
        public string? BillingBrand { get; private set; }
        public bool IsDeleted { get; private set; }

        public static Result<Expense> Create(ExpenseReport report, ExpenseDetails details)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentNullException.ThrowIfNull(details);

            var error = Validate(report, details);

            if (error is not null)
                return Result<Expense>.Failure(error);

            return Result<Expense>.Success(
                new Expense(Guid.NewGuid(), report.Id, details, isDeleted: false));
        }
        private static Error? Validate(ExpenseReport report, ExpenseDetails details)
        {
            if (string.IsNullOrWhiteSpace(details.Description) || details.Description.Length > 50)
                return ExpenseErrors.InvalidDescription;

            if (details.Amount <= 0 || decimal.Round(details.Amount, 2) != details.Amount)
                return ExpenseErrors.InvalidAmount;

            if (details.Date.Year != report.Year || details.Date.Month != report.Month)
                return ExpenseErrors.DateOutsideReportPeriod;

            if (string.IsNullOrWhiteSpace(details.BillingStreet))
                return ExpenseErrors.BillingStreetRequired;

            if (string.IsNullOrWhiteSpace(details.BillingPostalCode))
                return ExpenseErrors.BillingPostalCodeRequired;

            if (string.IsNullOrWhiteSpace(details.BillingCity))
                return ExpenseErrors.BillingCityRequired;

            return null;
        }
        public Result<Expense> Update(ExpenseReport destination, ExpenseDetails details)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(details);

            if (IsDeleted)
                return Result<Expense>.Failure(ExpenseErrors.Deleted);

            var error = Validate(destination, details);

            if (error is not null)
                return Result<Expense>.Failure(error);

            ExpenseReportId = destination.Id;
            Date = details.Date;
            Description = details.Description;
            Amount = details.Amount;
            BillingStreet = details.BillingStreet;
            BillingPostalCode = details.BillingPostalCode;
            BillingCity = details.BillingCity;
            BillingBrand = details.BillingBrand;

            return Result<Expense>.Success(this);
        }
        public Result<Expense> Delete()
        {
            if (IsDeleted)
                return Result<Expense>.Failure(ExpenseErrors.Deleted);

            IsDeleted = true;
            return Result<Expense>.Success(this);
        }

        // Restaure l’état persisté sans réappliquer les règles de création.
        internal static Expense Restore(Guid id, Guid expenseReportId, ExpenseDetails details, bool isDeleted) => new(id, expenseReportId, details, isDeleted);
    }
}
