using WebApi.Domain.Common;

namespace WebApi.Application.Common.Errors
{
    internal static class PaginationErrors
    {
        public static readonly Error InvalidPage =
            new("pagination.invalid_page", "Le numéro de page doit être supérieur ou égal à 1.");
    }
}
