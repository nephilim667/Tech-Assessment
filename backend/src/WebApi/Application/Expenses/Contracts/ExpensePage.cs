using WebApi.Application.Common.Pagination;
using WebApi.Domain.Models;

namespace WebApi.Application.Expenses.Contracts
{
    internal sealed record ExpensePage(PagedResult<Expense> Page, decimal TotalAmount);
}
