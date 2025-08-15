namespace KYCDocumentAPI.Infrastructure.Models
{
    public class DocumentExtractionResult
    {
        public bool Success { get; set; }
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
        public string? RawData { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
