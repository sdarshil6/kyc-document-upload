namespace KYCDocumentAPI.API.Models.DTOs
{
    public class FraudDetectionRequestDto
    {
        public IFormFile File { get; set; }
        public string DocumentType { get; set; } = "Other";
    }
}
