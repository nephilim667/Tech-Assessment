namespace WebApi.Infrastructure.Api.Expenses.Contracts
{
    internal sealed record ExpensePageResponse(IReadOnlyList<ExpenseResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, decimal TotalAmount);
}
