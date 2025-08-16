using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IDocumentValidationService
    {
        Task<DocumentValidationResult> ValidateDocumentAsync(Document document);
        Task<DocumentValidationResult> ValidateDocumentFromPathAsync(string filePath, DocumentType documentType);
        Task<VerificationMetrics> CalculateVerificationMetricsAsync(FraudDetectionInput input);
        Task<List<ValidationCheck>> RunSecurityChecksAsync(string filePath, DocumentType documentType);
        Task<bool> DetectTamperingAsync(string filePath);
        Task<float> CalculateAuthenticityScoreAsync(string extractedText, DocumentType documentType);
    }
}
