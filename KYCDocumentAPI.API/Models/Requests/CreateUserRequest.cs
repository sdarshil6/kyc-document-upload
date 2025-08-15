using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace KYCDocumentAPI.API.Models.Requests
{
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public State? State { get; set; }

        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid PIN code format")]
        public string? PinCode { get; set; }
    }
}
