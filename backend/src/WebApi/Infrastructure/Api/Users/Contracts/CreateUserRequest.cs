namespace WebApi.Infrastructure.Api.Users.Contracts
{
    internal sealed record CreateUserRequest(string FirstName, string LastName, string Street, string PostalCode, string City, int MaxExpensesPerMonth);
}