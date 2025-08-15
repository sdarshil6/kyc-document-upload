namespace KYCDocumentAPI.API.Models.DTOs
{
    public class DocumentDataDto
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? PassportNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public double ExtractionConfidence { get; set; }
    }
}
