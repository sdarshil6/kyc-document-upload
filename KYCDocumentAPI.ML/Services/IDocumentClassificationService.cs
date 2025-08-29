using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IDocumentClassificationService
    {
        Task<ImagePrediction> ClassifyDocumentAsync(string filePath);       
    }
}
