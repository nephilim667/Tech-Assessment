namespace WebApi.Infrastructure.Api.Expenses.Contracts
{
    internal sealed class ExpenseResponse
    {
        public required Guid Id { get; init; }
        public required Guid ExpenseReportId { get; init; }
        public required DateOnly Date { get; init; }
        public required string Description { get; init; }
        public required decimal Amount { get; init; }
        public required string BillingStreet { get; init; }
        public required string BillingPostalCode { get; init; }
        public required string BillingCity { get; init; }
        public string? BillingBrand { get; init; }
    }
}
