namespace KYCDocumentAPI.Infrastructure.Models
{
    public class FileStorageResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
