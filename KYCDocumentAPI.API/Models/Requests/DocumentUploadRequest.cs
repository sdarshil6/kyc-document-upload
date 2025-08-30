using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace KYCDocumentAPI.API.Models.Requests
{
    public class DocumentUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public DocumentType DocumentType { get; set; }          
    }
}
