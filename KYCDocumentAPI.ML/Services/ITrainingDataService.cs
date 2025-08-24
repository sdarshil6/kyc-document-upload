using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface ITrainingDataService
    {
        Task<List<ImageData>> LoadTrainingDataAsync(string dataPath);
        Task<bool> ValidateImageAsync(string imagePath);
        Task<TrainingDataStats> AnalyzeDatasetAsync(string dataPath);
        Task<List<ImageData>> AugmentDataAsync(List<ImageData> originalData, MLConfig config);
        Task<(List<ImageData> training, List<ImageData> validation)> SplitDataAsync(List<ImageData> data, float validationSplit);
    }
}
