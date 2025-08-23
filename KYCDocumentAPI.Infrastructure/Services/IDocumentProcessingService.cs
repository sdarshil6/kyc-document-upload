using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Models;
using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public interface IDocumentProcessingService
    {        
        Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType);        
        Task ProcessDocumentAsync(Guid documentId);
    }
}
