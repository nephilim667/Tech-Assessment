using WebApi.Infrastructure.Api.Expenses.Interfaces;

namespace WebApi.Infrastructure.Api.Expenses.Contracts
{
    internal sealed record UpdateExpenseRequest : IExpenseDetailsRequest
    {
        public Guid DestinationExpenseReportId { get; init; }
        public DateOnly? Date { get; init; }
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string BillingStreet { get; init; } = string.Empty;
        public string BillingPostalCode { get; init; } = string.Empty;
        public string BillingCity { get; init; } = string.Empty;
        public string? BillingBrand { get; init; }
    }
}
