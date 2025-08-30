using KYCDocumentAPI.Core.Entities;

namespace KYCDocumentAPI.API.Models.Responses
{
    public class DocumentUploadResponse
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;        
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public string InputDocumentType { get; set; } = string.Empty;
        public string ClassifiedDocumentType { get; set; } = string.Empty;

        public ExtractedDocumentData? ExtractedData { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public class ExtractedDocumentData
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? PassportNumber { get; set; }        
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public double ExtractionConfidence { get; set; }
    }
}
