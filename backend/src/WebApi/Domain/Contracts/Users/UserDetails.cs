namespace WebApi.Domain.Contracts.Users;

internal sealed record UserDetails(string FirstName, string LastName, string Street, string PostalCode, string City, int MaxExpensesPerMonth);
