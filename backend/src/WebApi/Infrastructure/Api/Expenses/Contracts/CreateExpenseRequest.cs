namespace WebApi.Infrastructure.Api.Expenses.Contracts
{
    internal sealed record CreateExpenseRequest(DateOnly? Date, string Description, decimal Amount, string BillingStreet, string BillingPostalCode, string BillingCity, string? BillingBrand = null);
}
