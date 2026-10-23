using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace WebApi.Infrastructure.Persistence.Entities
{
    [Table("expenses")]
    [Index(nameof(ExpenseReportId), nameof(Date), nameof(Id))]
    internal sealed class ExpenseEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ExpenseReportId { get; set; }

        public DateOnly Date { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Description { get; set; }

        [Column(TypeName = "numeric")]
        public decimal Amount { get; set; }

        [Required]
        public required string BillingStreet { get; set; }

        [Required]
        public required string BillingPostalCode { get; set; }

        [Required]
        public required string BillingCity { get; set; }

        public string? BillingBrand { get; set; }

        public bool IsDeleted { get; set; }
    }
}
