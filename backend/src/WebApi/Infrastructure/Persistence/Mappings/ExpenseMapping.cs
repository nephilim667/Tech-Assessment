using WebApi.Domain.Contracts.Expenses;
using WebApi.Domain.Models;

using WebApi.Infrastructure.Persistence.Entities;

namespace WebApi.Infrastructure.Persistence.Mappings
{
    internal static class ExpenseMapping
    {
        public static ExpenseEntity ToEntity(this Expense expense)
        {
            return new ExpenseEntity
            {
                Id = expense.Id,
                ExpenseReportId = expense.ExpenseReportId,
                Date = expense.Date,
                Description = expense.Description,
                Amount = expense.Amount,
                BillingStreet = expense.BillingStreet,
                BillingPostalCode = expense.BillingPostalCode,
                BillingCity = expense.BillingCity,
                BillingBrand = expense.BillingBrand,
                IsDeleted = expense.IsDeleted
            };
        }

        public static Expense ToDomain(this ExpenseEntity entity)
        {
            return Expense.Restore(
                id: entity.Id,
                expenseReportId: entity.ExpenseReportId,
                details: new ExpenseDetails(
                    Date: entity.Date,
                    Description: entity.Description,
                    Amount: entity.Amount,
                    BillingStreet: entity.BillingStreet,
                    BillingPostalCode: entity.BillingPostalCode,
                    BillingCity: entity.BillingCity,
                    BillingBrand: entity.BillingBrand),
                isDeleted: entity.IsDeleted);
        }

        public static void ApplyStateTo(this Expense expense, ExpenseEntity entity)
        {
            if (expense.Id != entity.Id)
                throw new InvalidOperationException("Les identifiants de la dépense et de l’entité doivent correspondre.");

            entity.IsDeleted = expense.IsDeleted;
        }
    }
}
