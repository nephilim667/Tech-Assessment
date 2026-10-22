using WebApi.Domain.Common;

namespace WebApi.Infrastructure.Api.Users.Errors
{
    internal static class UserRequestErrors
    {
        public static readonly Error StatusRequired = new(
            "user.status_required",
            "L’état actif ou inactif est obligatoire.");
    }
}
