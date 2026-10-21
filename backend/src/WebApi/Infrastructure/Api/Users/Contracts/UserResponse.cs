namespace WebApi.Infrastructure.Api.Users.Contracts
{
    internal sealed record UserResponse
    {
        public required Guid Id { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string Street { get; init; }
        public required string PostalCode { get; init; }
        public required string City { get; init; }
        public required int MaxExpensesPerMonth { get; init; }
        public required bool IsActive { get; init; }
    }
}
