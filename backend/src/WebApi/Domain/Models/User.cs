using WebApi.Domain.Common;

namespace WebApi.Domain.Models;

internal sealed class User
{
    // Constructeur privé pour forcer le passage par la factory.
    private User(string firstName, string lastName, string street, string postalCode, string city, int maxExpensesPerMonth)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        PostalCode = postalCode;
        City = city;
        MaxExpensesPerMonth = maxExpensesPerMonth;
    }

    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string PostalCode { get; }
    public string City { get; }
    public int MaxExpensesPerMonth { get; }

    // La factory protège les invariants du modèle métier
    public static Result<User> Create(string firstName, string lastName, string street, string postalCode, string city, int maxExpensesPerMonth)
    {
        if (maxExpensesPerMonth < 1)
            return Result<User>.Failure(new Error(
                "user.invalid_monthly_quota", "Le quota mensuel doit être supérieur ou égal à 1."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure(new Error(
                "user.invalid_first_name", "Le prénom est obligatoire."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure(new Error(
                "user.invalid_last_name", "Le nom est obligatoire."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure(new Error(
                "user.invalid_last_name", "Le nom est obligatoire."));

        if (string.IsNullOrWhiteSpace(street))
            return Result<User>.Failure(new Error(
                "user.invalid_street", "La rue est obligatoire."));

        if (string.IsNullOrWhiteSpace(postalCode))
            return Result<User>.Failure(new Error(
                "user.invalid_postal_code", "Le code postal est obligatoire."));

        if (string.IsNullOrWhiteSpace(city))
            return Result<User>.Failure(new Error(
                "user.invalid_city", "La ville est obligatoire."));

        return Result<User>.Success(new User(firstName, lastName, street, postalCode, city, maxExpensesPerMonth));
    }
}