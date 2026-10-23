using WebApi.Domain.Models;
using WebApi.Infrastructure.Api.Expenses.Contracts;

namespace WebApi.Infrastructure.Api.Expenses.Mappings
{
    internal static class ExpenseResponseMappings
    {
        public static ExpenseResponse ToResponse(this Expense expense) =>
            new()
            {
                Id = expense.Id,
                ExpenseReportId = expense.ExpenseReportId,
                Date = expense.Date,
                Description = expense.Description,
                Amount = expense.Amount,
                BillingStreet = expense.BillingStreet,
                BillingPostalCode = expense.BillingPostalCode,
                BillingCity = expense.BillingCity,
                BillingBrand = expense.BillingBrand
            };
    }
}
