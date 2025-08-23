namespace KYCDocumentAPI.API.Models.Requests
{
    public class SmartProcessRequest
    {
        public IFormFile File { get; set; }
        public string DocumentType { get; set; } = "unknown";
    }
}
