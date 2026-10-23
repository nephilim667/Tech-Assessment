using WebApi.Domain.Contracts.Expenses;
using WebApi.Infrastructure.Api.Expenses.Contracts;

namespace WebApi.Infrastructure.Api.Expenses.Mappings
{
    internal static class ExpenseRequestMappings
    {
        public static ExpenseDetails ToDetails(this CreateExpenseRequest request) =>
            new(Date: request.Date!.Value,
                Description: request.Description,
                Amount: request.Amount,
                BillingStreet: request.BillingStreet,
                BillingPostalCode: request.BillingPostalCode,
                BillingCity: request.BillingCity,
                BillingBrand: request.BillingBrand);

        public static ExpenseDetails ToDetails(this UpdateExpenseRequest request) =>
            new(Date: request.Date!.Value,
                Description: request.Description,
                Amount: request.Amount,
                BillingStreet: request.BillingStreet,
                BillingPostalCode: request.BillingPostalCode,
                BillingCity: request.BillingCity,
                BillingBrand: request.BillingBrand);
    }
}
