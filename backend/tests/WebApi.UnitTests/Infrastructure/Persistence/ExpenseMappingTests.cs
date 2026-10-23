using WebApi.Domain.Models;
using WebApi.Domain.Contracts.Expenses;
using WebApi.Infrastructure.Persistence.Entities;
using WebApi.Infrastructure.Persistence.Mappings;

namespace WebApi.UnitTests.Infrastructure.Persistence
{
    public sealed class ExpenseMappingTests
    {
        [Fact]
        public void ToEntity_Preserves_Domain_Data()
        {
            var user = User.Create(
                "Juste", "Leblanc",
                "10 rue des Lilas", "75001", "Paris", 5).Value;

            var report = ExpenseReport.Create(user, 2026, 10).Value;

            var details = new ExpenseDetails(
                Date: new DateOnly(2026, 10, 15),
                Description: "Déjeuner",
                Amount: 25.50m,
                BillingStreet: "20 rue du Restaurant",
                BillingPostalCode: "69001",
                BillingCity: "Lyon",
                BillingBrand: "Restaurant du Centre");

            var expense = Expense.Create(report, details).Value;

            var entity = expense.ToEntity();

            Assert.Equal(expense.Id, entity.Id);
            Assert.Equal(expense.ExpenseReportId, entity.ExpenseReportId);
            Assert.Equal(expense.Date, entity.Date);
            Assert.Equal(expense.Description, entity.Description);
            Assert.Equal(expense.Amount, entity.Amount);
            Assert.Equal(expense.BillingStreet, entity.BillingStreet);
            Assert.Equal(expense.BillingPostalCode, entity.BillingPostalCode);
            Assert.Equal(expense.BillingCity, entity.BillingCity);
            Assert.Equal(expense.BillingBrand, entity.BillingBrand);
            Assert.Equal(expense.IsDeleted, entity.IsDeleted);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ToDomain_Preserves_Persisted_Data(bool isDeleted)
        {
            var entity = new ExpenseEntity
            {
                Id = Guid.NewGuid(),
                ExpenseReportId = Guid.NewGuid(),
                Date = new DateOnly(2026, 10, 15),
                Description = "Déjeuner",
                Amount = 25.50m,
                BillingStreet = "20 rue du Restaurant",
                BillingPostalCode = "69001",
                BillingCity = "Lyon",
                BillingBrand = null,
                IsDeleted = isDeleted
            };

            var expense = entity.ToDomain();

            Assert.Equal(entity.Id, expense.Id);
            Assert.Equal(entity.ExpenseReportId, expense.ExpenseReportId);
            Assert.Equal(entity.Date, expense.Date);
            Assert.Equal(entity.Description, expense.Description);
            Assert.Equal(entity.Amount, expense.Amount);
            Assert.Equal(entity.BillingStreet, expense.BillingStreet);
            Assert.Equal(entity.BillingPostalCode, expense.BillingPostalCode);
            Assert.Equal(entity.BillingCity, expense.BillingCity);
            Assert.Null(expense.BillingBrand);
            Assert.Equal(isDeleted, expense.IsDeleted);
        }
    }
}
