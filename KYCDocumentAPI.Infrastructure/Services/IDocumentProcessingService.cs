using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.DTOs;
using KYCDocumentAPI.Infrastructure.Models;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public interface IDocumentProcessingService
    {        
        Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType);
        Task<ProcessedDocumentDto> ProcessDocumentAsync(Guid documentId);
    }
}
