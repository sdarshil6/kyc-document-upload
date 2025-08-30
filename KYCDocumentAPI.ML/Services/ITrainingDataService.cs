using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface ITrainingDataService
    {
        Task<List<ImageData>> LoadTrainingDataAsync(string dataPath);       
        Task<TrainingDataStats> AnalyzeDatasetAsync(string dataPath);       
    }
}
