using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Infrastructure.Persistence.Entities
{
    [Table("expense_reports")]
    [Index(nameof(UserId), nameof(Year), nameof(Month))]
    internal sealed class ExpenseReportEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Required]
        public required string Title { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }
    }
}
