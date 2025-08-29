using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IMLModelTrainingService
    {        
        Task<bool> IsModelTrainedAsync();
        Task<bool> LoadModelAsync(string? modelPath = null);              
        Task<TrainingMetrics> TrainModelAsync();        
        Task<ImagePrediction> PredictAsync(string imagePath);       
        bool IsModelLoaded { get; }     
        MLConfig CurrentConfig { get; }       
    }
}
