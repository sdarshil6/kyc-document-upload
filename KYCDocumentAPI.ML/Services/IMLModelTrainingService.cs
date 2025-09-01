using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IMLModelTrainingService
    {
        Task<bool> IsModelTrainedAsync();
        Task<bool> LoadModelAsync();
        Task<TrainingMetrics> TrainModelAsync(bool limitedToAadhaarPan = false);
        Task<ImagePrediction> PredictAsync(string imagePath);
        bool IsModelLoaded { get; }
        MLConfig CurrentConfig { get; }
    }
}
