using KYCDocumentAPI.Core.Entities;

namespace KYCDocumentAPI.Infrastructure.DTOs
{
    public class ProcessedDocumentDto
    {
        
        public ProcessedDocumentDto()
        {
            Document = new Document();
            DocumentData = new DocumentData();
            IsRejected = false;
            Message = string.Empty;
        }

        public Document? Document { get; set; }
        public DocumentData? DocumentData { get; set; }
        public bool IsRejected { get; set; }
        public string Message { get; set; }
    }
}
