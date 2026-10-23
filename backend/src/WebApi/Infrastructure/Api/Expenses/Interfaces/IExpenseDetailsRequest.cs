namespace WebApi.Infrastructure.Api.Expenses.Interfaces
{
    internal interface IExpenseDetailsRequest
    {
        DateOnly? Date { get; }
        string Description { get; }
        decimal Amount { get; }
        string BillingStreet { get; }
        string BillingPostalCode { get; }
        string BillingCity { get; }
        string? BillingBrand { get; }
    }
}
