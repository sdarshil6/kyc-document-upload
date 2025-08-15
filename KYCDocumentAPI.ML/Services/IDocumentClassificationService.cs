using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IDocumentClassificationService
    {
        Task<DocumentClassificationResult> ClassifyDocumentAsync(string filePath, string fileName = "");
        Task InitializeModelAsync();
        bool IsModelReady { get; }
    }
}
