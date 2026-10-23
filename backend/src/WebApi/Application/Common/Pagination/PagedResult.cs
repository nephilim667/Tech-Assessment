namespace WebApi.Application.Common.Pagination
{
    internal sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
    {
        public int TotalPages => (int)((TotalCount + (long)PageSize - 1) / PageSize);
    }
}
