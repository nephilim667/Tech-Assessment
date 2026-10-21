using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Infrastructure.Persistence.Entities
{
    [Table("users")]
    internal sealed class UserEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public required string FirstName { get; set; }

        [Required]
        public required string LastName { get; set; }

        [Required]
        public required string Street { get; set; }

        [Required]
        public required string PostalCode { get; set; }

        [Required]
        public required string City { get; set; }

        public int MaxExpensesPerMonth { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
    }
}
