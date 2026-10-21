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
    }
}
