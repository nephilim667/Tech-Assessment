namespace WebApi.Infrastructure.Api.Administrators.Contracts
{
    internal sealed record CreateAdministratorAccountRequest(string Email, string Password);
}
