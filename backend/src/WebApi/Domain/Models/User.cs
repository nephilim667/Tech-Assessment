using WebApi.Domain.Common;
using WebApi.Domain.Contracts.Users;
using WebApi.Domain.Errors;

namespace WebApi.Domain.Models;

internal sealed class User
{
    // Constructeur privé pour forcer le passage par les factory create/restore.
    private User(Guid id, UserDetails details, bool isActive, bool isDeleted)
    {
        Id = id;
        FirstName = details.FirstName;
        LastName = details.LastName;
        Street = details.Street;
        PostalCode = details.PostalCode;
        City = details.City;
        MaxExpensesPerMonth = details.MaxExpensesPerMonth;
        IsActive = isActive;
        IsDeleted = isDeleted;
    }

    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string PostalCode { get; }
    public string City { get; }
    public int MaxExpensesPerMonth { get; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // La factory de creation protège les invariants du modèle métier
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

        UserDetails details = new (firstName, lastName, street, postalCode, city, maxExpensesPerMonth);

        return Result<User>.Success(new User(Guid.NewGuid(), details, isActive: true, isDeleted: false));
    }
    // La factory de restoration protège les invariants du modèle métier
    internal static User Restore(Guid id, UserDetails details, bool isActive, bool isDeleted) =>
        new (id, details, isActive, isDeleted);

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