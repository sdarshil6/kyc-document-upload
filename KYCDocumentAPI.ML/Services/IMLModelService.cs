using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IMLModelService
    {        
        Task<bool> IsModelTrainedAsync();
        Task<bool> LoadModelAsync(string? modelPath = null);
        Task<ModelEvaluationResult> GetModelMetricsAsync();
        Task<string> GetModelInfoAsync();        
        Task<TrainingMetrics> TrainModelAsync(MLConfig? config = null, IProgress<TrainingProgress>? progress = null);
        Task<ModelEvaluationResult> EvaluateModelAsync(string? testDataPath = null);
        Task<bool> ValidateTrainingDataAsync();        
        Task<DocumentClassificationResult> ClassifyDocumentAsync(string imagePath);
        Task<List<DocumentClassificationResult>> ClassifyBatchAsync(IEnumerable<string> imagePaths);
        Task<ImagePrediction> PredictAsync(string imagePath);       
        bool IsModelLoaded { get; }
        DateTime? LastTrainingDate { get; }
        string? ModelVersion { get; }
        MLConfig CurrentConfig { get; }        
        Task<bool> PrepareTrainingDataAsync();
        Task<TrainingMetrics> GetTrainingHistoryAsync();
        Task<bool> BackupModelAsync(string backupPath);
        Task<bool> RestoreModelAsync(string backupPath);
    }
}
