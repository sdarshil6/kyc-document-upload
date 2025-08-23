using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace KYCDocumentAPI.Core.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        public State? State { get; set; }

        [MaxLength(10)]
        public string? PinCode { get; set; }
        
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();        
    }
}
