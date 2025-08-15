using KYCDocumentAPI.Core.Enums;

namespace KYCDocumentAPI.API.Models.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DocumentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DocumentDataDto? ExtractedData { get; set; }
        public VerificationResultDto? LatestVerification { get; set; }
    }
}
