using WebApi.Domain.Common;
using WebApi.Domain.Errors;

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
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; }

    // La factory protège les invariants du modèle métier
    public static Result<User> Create(string firstName, string lastName, string street, string postalCode, string city, int maxExpensesPerMonth)
    {
        if (maxExpensesPerMonth < 1)
            return Result<User>.Failure(UserErrors.InvalidMonthlyQuota);

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure(UserErrors.InvalidFirstName);

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure(UserErrors.InvalidLastName);

        if (string.IsNullOrWhiteSpace(street))
            return Result<User>.Failure(UserErrors.InvalidStreet);

        if (string.IsNullOrWhiteSpace(postalCode))
            return Result<User>.Failure(UserErrors.InvalidPostalCode);

        if (string.IsNullOrWhiteSpace(city))
            return Result<User>.Failure(UserErrors.InvalidCity);

        return Result<User>.Success(new User(firstName, lastName, street, postalCode, city, maxExpensesPerMonth));
    }

    // Acces aux proriétés privées d'activation/désactivation
    public Result<User> Deactivate()
    {
        if (IsDeleted)
            return Result<User>.Failure(UserErrors.Deleted);

        IsActive = false;
        return Result<User>.Success(this);
    }

    public Result<User> Activate()
    {
        if (IsDeleted)
            return Result<User>.Failure(UserErrors.Deleted);

        IsActive = true;
        return Result<User>.Success(this);
    }

    // Accés à la suppression logique d'un utilisateur
    public Result<User> Delete()
    {
        if (IsDeleted)
            return Result<User>.Failure(UserErrors.Deleted);

        IsDeleted = true;
        IsActive = false;
        return Result<User>.Success(this);
    }
}