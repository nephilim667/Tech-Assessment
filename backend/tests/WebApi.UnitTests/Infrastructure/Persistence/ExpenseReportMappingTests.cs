using WebApi.Domain.Models;

using WebApi.Infrastructure.Persistence.Entities;
using WebApi.Infrastructure.Persistence.Mappings;

namespace WebApi.UnitTests.Infrastructure.Persistence
{
    public sealed class ExpenseReportMappingTests
    {
        [Fact]
        public void ToEntity_Preserves_Domain_Data()
        {
            // Arrange
            var user = User.Create(
                "Juste",
                "Leblanc",
                "10 rue des Lilas",
                "75001",
                "Paris",
                5).Value;

            var report = ExpenseReport.Create(user, 2026, 10).Value;

            // Act
            var entity = report.ToEntity();

            // Assert
            Assert.Equal(report.Id, entity.Id);
            Assert.Equal(report.UserId, entity.UserId);
            Assert.Equal(report.Title, entity.Title);
            Assert.Equal(report.Year, entity.Year);
            Assert.Equal(report.Month, entity.Month);
        }

        [Fact]
        public void ToDomain_Preserves_Persisted_Data_And_Historical_Title()
        {
            // Arrange
            var entity = new ExpenseReportEntity
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Intitulé historique conservé",
                Year = 2026,
                Month = 10
            };

            // Act
            var report = entity.ToDomain();

            // Assert
            Assert.Equal(entity.Id, report.Id);
            Assert.Equal(entity.UserId, report.UserId);
            Assert.Equal(entity.Title, report.Title);
            Assert.Equal(entity.Year, report.Year);
            Assert.Equal(entity.Month, report.Month);
        }
    }
}
