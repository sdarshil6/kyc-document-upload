using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Models;
using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public interface IDocumentProcessingService
    {
        Task<DocumentClassificationResult> ClassifyDocumentAsync(string filePath);
        Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType);
        Task<VerificationResult> VerifyDocumentAsync(Guid documentId);
        Task ProcessDocumentAsync(Guid documentId);
    }
}
