using KYCDocumentAPI.Core.Enums;

namespace KYCDocumentAPI.API.Models.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public State? State { get; set; }
        public string? PinCode { get; set; }
        public int DocumentsCount { get; set; }      
        public DateTime CreatedAt { get; set; }
    }
}
