namespace WebApi.Domain.Models;

internal sealed record UserDetails(string FirstName, string LastName, string Street, string PostalCode, string City, int MaxExpensesPerMonth);
