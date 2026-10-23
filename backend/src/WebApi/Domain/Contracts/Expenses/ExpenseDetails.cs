namespace WebApi.Domain.Contracts.Expenses
{
    internal sealed record ExpenseDetails(DateOnly Date, string Description, decimal Amount, string BillingStreet, string BillingPostalCode, string BillingCity, string? BillingBrand = null);
}
