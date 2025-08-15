namespace KYCDocumentAPI.API.Models.Responses
{
    public class DocumentUploadResponse
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ProcessingStarted { get; set; }
    }
}
