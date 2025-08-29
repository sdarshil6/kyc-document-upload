using KYCDocumentAPI.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System.Diagnostics;
using System.Text.Json;

namespace KYCDocumentAPI.ML.Services
{
    public class MLModelTrainingService : IMLModelTrainingService
    {
        private readonly MLContext _mlContext;
        private readonly ILogger<MLModelTrainingService> _logger;
        private readonly ITrainingDataService _trainingDataService;
        private ITransformer? _trainedModel;
        private PredictionEngine<ImageData, ImagePrediction>? _predictionEngine;
        private MLConfig _currentConfig;
        private TrainingMetrics? _lastTrainingMetrics;

        public bool IsModelLoaded => _trainedModel != null && _predictionEngine != null;        
        public MLConfig CurrentConfig => _currentConfig;

        public MLModelTrainingService(ILogger<MLModelTrainingService> logger, ITrainingDataService trainingDataService)
        {
            _mlContext = new MLContext(seed: 1);
            _logger = logger;
            _trainingDataService = trainingDataService;
            _currentConfig = MLConfig.GetDevelopmentConfig();
        }
       
        public async Task<TrainingMetrics> TrainModelAsync()
        {
            var trainingConfig = _currentConfig;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting ML.NET model training with config: {Config}", JsonSerializer.Serialize(trainingConfig));
                
                var configErrors = trainingConfig.ValidateConfiguration();
                if (configErrors.Any())                
                    throw new ArgumentException($"Invalid configuration: {string.Join(", ", configErrors)}");
                
                var metrics = new TrainingMetrics
                {
                    ModelName = "DocumentClassifier",
                    TrainingStartTime = DateTime.UtcNow,
                    Configuration = trainingConfig
                };                                

                var allImages = await _trainingDataService.LoadTrainingDataAsync(trainingConfig.TrainingDataPath);
                if (allImages.Count < 5)                
                    throw new InvalidOperationException($"Insufficient training data: {allImages.Count} images. Need at least 50.");                                               
                
                metrics.TotalTrainingImages = allImages.Count;                
                metrics.NumberOfClasses = allImages.GroupBy(x => x.Label).Count();
                
                foreach (var group in allImages.GroupBy(x => x.Label))                
                    metrics.ImagesPerClass[group.Key] = group.Count();
                
                _logger.LogInformation("Training set: {TrainingCount} images", allImages.Count);                
                
                var trainingDataView = _mlContext.Data.LoadFromEnumerable(allImages);                                               

                var pipeline = BuildTrainingPipeline(trainingConfig);                                

                _logger.LogInformation("Starting neural network training...");
               
                var trainer = pipeline.Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(featureColumnName: "ImagePath", labelColumnName: "Label").Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel")));                               

                _trainedModel = trainer.Fit(trainingDataView);

                stopwatch.Stop();
                metrics.TrainingEndTime = DateTime.UtcNow;                               

                await SaveModelAsync(trainingConfig.ModelOutputPath);

                // Create prediction engine
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(_trainedModel);
                
                metrics.ValidateQuality();
                
                _currentConfig = trainingConfig;
                _lastTrainingMetrics = metrics;

                _logger.LogInformation("Model training completed: {Summary}", metrics.GetTrainingSummary());
               
                return metrics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during model training");                

                throw new InvalidOperationException($"Model training failed: {ex.Message}", ex);
            }
        }
        
        private EstimatorChain<ValueToKeyMappingTransformer> BuildTrainingPipeline(MLConfig config)
        {
            try
            {
                _logger.LogInformation("Building ML.NET pipeline with architecture: {Architecture}", config.Architecture);

                var pipeline = _mlContext.Transforms.LoadImages(outputColumnName: "Image", imageFolder: "", inputColumnName: "ImagePath")
                    .Append(_mlContext.Transforms.ResizeImages(outputColumnName: "ImageResized", inputColumnName: "Image", imageWidth: config.ImageWidth, imageHeight: config.ImageHeight))
                    .Append(_mlContext.Transforms.ExtractPixels(outputColumnName: "Pixels", inputColumnName: "ImageResized"))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                    .AppendCacheCheckpoint(_mlContext);

                return pipeline;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside BuildTrainingPipeline() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }
        
        public async Task<bool> LoadModelAsync(string? modelPath = null)
        {
            try
            {
                var pathToLoad = modelPath ?? _currentConfig.ModelOutputPath;

                if (!File.Exists(pathToLoad))
                {
                    _logger.LogWarning("Model file not found: {ModelPath}", pathToLoad);
                    return false;
                }

                _trainedModel = _mlContext.Model.Load(pathToLoad, out var modelInputSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(_trainedModel);

                _logger.LogInformation("Model loaded successfully from: {ModelPath}", pathToLoad);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model from: {ModelPath}", modelPath);
                return false;
            }
        }
        
        public async Task<bool> IsModelTrainedAsync()
        {
            try
            {
                await Task.CompletedTask;
                return File.Exists(_currentConfig.ModelOutputPath) || IsModelLoaded;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside IsModelTrainedAsync() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }
        
        public async Task<DocumentClassificationResult> ClassifyDocumentAsync(string imagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModelAsync();
                if (!IsModelLoaded)
                {
                    throw new InvalidOperationException("No trained model available. Please train a model first.");
                }
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var imageData = new ImageData
                {
                    ImagePath = imagePath,
                    OriginalFileName = Path.GetFileName(imagePath)
                };

                var prediction = _predictionEngine!.Predict(imageData);
                stopwatch.Stop();                              

                var result = new DocumentClassificationResult
                {
                    PredictedDocumentType = prediction.PredictedLabel,
                    Confidence = prediction.Confidence,
                    AllProbabilities = prediction.GetAllProbabilities(),
                    IsConfident = prediction.IsConfident(_currentConfig.MinimumConfidenceThreshold),
                    ProcessingTime = stopwatch.Elapsed,                                    
                    RequiresManualReview = !prediction.IsConfident(_currentConfig.MinimumConfidenceThreshold)                    
                };
                
                result.ConfidenceFactors = GenerateConfidenceFactors(prediction);

                _logger.LogDebug("Document classified: {Result}", result.GetSummary());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error classifying document: {ImagePath}", imagePath);

                return new DocumentClassificationResult
                {
                    PredictedDocumentType = "Other",
                    Confidence = 0f,
                    ProcessingTime = stopwatch.Elapsed,
                    ProcessingNotes = $"Classification error: {ex.Message}",
                    RequiresManualReview = true
                };
            }
        }
       
        public async Task<ImagePrediction> PredictAsync(string imagePath)
        {
            try
            {
                if (!IsModelLoaded)
                {
                    await LoadModelAsync();
                    if (!IsModelLoaded)                    
                        throw new InvalidOperationException("No trained model available.");                   
                }

                var imageData = new ImageData
                {
                    ImagePath = imagePath,
                    OriginalFileName = Path.GetFileName(imagePath)
                };

                return _predictionEngine!.Predict(imageData);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside PredictAsync() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }
        
        public async Task<List<DocumentClassificationResult>> ClassifyBatchAsync(IEnumerable<string> imagePaths)
        {
            try
            {
                var results = new List<DocumentClassificationResult>();

                foreach (var imagePath in imagePaths)
                {
                    try
                    {
                        var result = await ClassifyDocumentAsync(imagePath);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in batch classification for: {ImagePath}", imagePath);
                        results.Add(new DocumentClassificationResult
                        {
                            PredictedDocumentType = "Other",
                            Confidence = 0f,
                            ProcessingNotes = $"Batch processing error: {ex.Message}",
                            RequiresManualReview = true
                        });
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside ClassifyBatchAsync() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }                
       
        private async Task SaveModelAsync(string modelPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _mlContext.Model.Save(_trainedModel!, null, modelPath);

                var fileInfo = new FileInfo(modelPath);
                if (_lastTrainingMetrics != null)
                {
                    _lastTrainingMetrics.ModelFilePath = modelPath;
                    _lastTrainingMetrics.ModelFileSize = fileInfo.Length;
                }

                _logger.LogInformation("Model saved to: {ModelPath} ({Size} bytes)", modelPath, fileInfo.Length);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving model to: {ModelPath}", modelPath);
                throw;
            }
        }                

        private List<string> GenerateConfidenceFactors(ImagePrediction prediction)
        {
            try
            {
                var factors = new List<string>();

                if (prediction.Confidence > 0.8f)
                    factors.Add("Strong model confidence");

                var secondBest = prediction.Scores?.OrderByDescending(x => x).Skip(1).FirstOrDefault() ?? 0f;
                var confidenceGap = prediction.Confidence - secondBest;

                if (confidenceGap > 0.3f)
                    factors.Add("Clear distinction from other document types");

                return factors;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GenerateConfidenceFactors() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }                               
    }       
}