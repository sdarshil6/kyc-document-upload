using KYCDocumentAPI.ML.Models;
using Microsoft.AspNetCore.Hosting;
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

        // Model + concurrency control
        private ITransformer? _trainedModel;
        private readonly ReaderWriterLockSlim _modelLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private MLConfig _currentConfig;
        private TrainingMetrics? _lastTrainingMetrics;
        private readonly IWebHostEnvironment _env;

        public bool IsModelLoaded
        {
            get
            {
                _modelLock.EnterReadLock();
                try { return _trainedModel != null; }
                finally { _modelLock.ExitReadLock(); }
            }
        }

        public MLConfig CurrentConfig => _currentConfig;

        public MLModelTrainingService(ILogger<MLModelTrainingService> logger, ITrainingDataService trainingDataService, IWebHostEnvironment env)
        {
            _mlContext = new MLContext(seed: 1);
            _logger = logger;
            _trainingDataService = trainingDataService;
            _currentConfig = MLConfig.GetDevelopmentConfig();
            _env = env;
        }

        public async Task<TrainingMetrics> TrainModelAsync(bool isLimitedToFewDocumentTypes = false)
        {
            var trainingConfig = _currentConfig;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                string mode = isLimitedToFewDocumentTypes ? "Aadhaar + PAN only" : "All classes";
                string modelName = isLimitedToFewDocumentTypes ? "DocumentClassifier_Limited" : "DocumentClassifier";

                _logger.LogInformation("Starting ML.NET model training ({Mode}) with config: {Config}", mode, JsonSerializer.Serialize(trainingConfig));

                var configErrors = trainingConfig.ValidateConfiguration();
                if (configErrors.Any())
                    throw new ArgumentException($"Invalid configuration: {string.Join(", ", configErrors)}");

                var metrics = new TrainingMetrics
                {
                    ModelName = modelName,
                    TrainingStartTime = DateTime.UtcNow,
                    Configuration = trainingConfig
                };

                var allImages = await _trainingDataService.LoadTrainingDataAsync(trainingConfig.TrainingDataPath);

                var dataset = isLimitedToFewDocumentTypes
                    ? allImages.Where(x =>
                        string.Equals(x.Label, "Aadhaar Regular", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Label, "Aadhaar Front", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Label, "Aadhaar Back", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Label, "PAN", StringComparison.OrdinalIgnoreCase))
                        .ToList()
                    : allImages?.ToList();

                if (dataset == null || dataset.Count == 0)
                    throw new InvalidOperationException("No training images found in the configured training path.");
                
                var classesCount = dataset.GroupBy(x => x.Label).Count();
                if (classesCount < 2)
                    throw new InvalidOperationException($"Insufficient distinct classes to train (found {classesCount}).");

                metrics.TotalTrainingImages = dataset.Count;
                metrics.NumberOfClasses = classesCount;
                foreach (var group in dataset.GroupBy(x => x.Label))
                    metrics.ImagesPerClass[group.Key] = group.Count();

                _logger.LogInformation("Training set: {Count} images ({Mode})", dataset.Count, mode);

                var allDataView = _mlContext.Data.LoadFromEnumerable(dataset);
                var split = _mlContext.Data.TrainTestSplit(allDataView, testFraction: 0.2, seed: 123);
                var trainSet = split.TrainSet;
                var validationSet = split.TestSet;
               
                var pipeline = BuildTrainingPipeline(trainingConfig)
                    .Append(_mlContext.MulticlassClassification.Trainers
                        .ImageClassification(featureColumnName: "Image", labelColumnName: "Label"))                    
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));

                _logger.LogInformation("Starting neural network training...");

                ITransformer fitted = pipeline.Fit(trainSet);

                _modelLock.EnterWriteLock();
                try
                {
                    _trainedModel = fitted;
                }
                finally
                {
                    _modelLock.ExitWriteLock();
                }

                stopwatch.Stop();
                metrics.TrainingEndTime = DateTime.UtcNow;

                await SaveModelAsync(trainingConfig.ModelOutputPath);
                
                float trainAcc;
                {
                    ITransformer? modelSnapshot;
                    _modelLock.EnterReadLock();
                    try { modelSnapshot = _trainedModel; }
                    finally { _modelLock.ExitReadLock(); }

                    var trainPredictions = modelSnapshot!.Transform(trainSet);
                    var trainEval = _mlContext.MulticlassClassification.Evaluate(
                        trainPredictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");
                    trainAcc = (float)trainEval.MacroAccuracy;
                }
                
                float valAcc;
                {
                    ITransformer? modelSnapshot;
                    _modelLock.EnterReadLock();
                    try { modelSnapshot = _trainedModel; }
                    finally { _modelLock.ExitReadLock(); }

                    var valPredictions = modelSnapshot!.Transform(validationSet);
                    var valEval = _mlContext.MulticlassClassification.Evaluate(
                        valPredictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");
                    valAcc = (float)valEval.MacroAccuracy;
                }

                metrics.ValidationAccuracy = valAcc;
                metrics.FinalAccuracy = valAcc;

                metrics.ValidateQuality();

                _currentConfig = trainingConfig;
                _lastTrainingMetrics = metrics;

                _logger.LogInformation("Model training completed ({Mode}). Train Acc={TrainAcc:P2}, Val Acc={ValAcc:P2}", mode, trainAcc, valAcc);

                return metrics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during model training");
                throw new InvalidOperationException($"Model training failed: {ex.Message}", ex);
            }
        }


        private IEstimator<ITransformer> BuildTrainingPipeline(MLConfig config)
        {
            try
            {
                _logger.LogInformation("Building ML.NET pipeline with architecture: {Architecture}", config.Architecture);

                var pipeline = _mlContext.Transforms.LoadRawImageBytes(
                        outputColumnName: "Image",
                        imageFolder: "",
                        inputColumnName: nameof(ImageData.ImagePath))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                    .AppendCacheCheckpoint(_mlContext);                

                return pipeline;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside BuildTrainingPipeline() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }

        public Task<bool> LoadModelAsync()
        {
            var pathToLoad = string.Empty;
            try
            {
                pathToLoad = Path.Combine(_env.ContentRootPath, "Machine Learning Models", "Trained", "DocumentClassifier.zip");

                if (!File.Exists(pathToLoad))
                {
                    _logger.LogWarning("Model file not found: {ModelPath}", pathToLoad);
                    return Task.FromResult(false);
                }

                var loaded = _mlContext.Model.Load(pathToLoad, out var _);

                _modelLock.EnterWriteLock();
                try
                {
                    _trainedModel = loaded;
                }
                finally
                {
                    _modelLock.ExitWriteLock();
                }

                _logger.LogInformation("Model loaded successfully from: {ModelPath}", pathToLoad);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model from: {ModelPath}", pathToLoad);
                return Task.FromResult(false);
            }
        }

        public Task<bool> IsModelTrainedAsync()
        {
            try
            {
                bool hasFile = File.Exists(_currentConfig.ModelOutputPath);
                bool isLoaded = IsModelLoaded;
                return Task.FromResult(hasFile || isLoaded);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside IsModelTrainedAsync() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }

        public async Task<ImagePrediction> PredictAsync(string imagePath)
        {
            try
            {
                if (!IsModelLoaded)
                {
                    var loaded = await LoadModelAsync();
                    if (!loaded && !IsModelLoaded)
                        throw new InvalidOperationException("No trained model available.");
                }

                ITransformer? modelSnapshot;
                _modelLock.EnterReadLock();
                try { modelSnapshot = _trainedModel; }
                finally { _modelLock.ExitReadLock(); }

                if (modelSnapshot == null)
                    throw new InvalidOperationException("No trained model available.");

                using var engine = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(modelSnapshot);

                var imageData = new ImageData
                {
                    ImagePath = imagePath,
                    OriginalFileName = Path.GetFileName(imagePath)
                };

                return engine.Predict(imageData);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside PredictAsync() in MLModelTrainingService.cs : " + ex);
                throw;
            }
        }

        private Task SaveModelAsync(string modelPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                ITransformer? modelSnapshot;
                _modelLock.EnterReadLock();
                try { modelSnapshot = _trainedModel; }
                finally { _modelLock.ExitReadLock(); }

                if (modelSnapshot == null)
                    throw new InvalidOperationException("Cannot save model: no trained model in memory.");

                _mlContext.Model.Save(modelSnapshot, inputSchema: null, filePath: modelPath);

                var fileInfo = new FileInfo(modelPath);
                if (_lastTrainingMetrics != null)
                {
                    _lastTrainingMetrics.ModelFilePath = modelPath;
                    _lastTrainingMetrics.ModelFileSize = fileInfo.Length;
                }

                _logger.LogInformation("Model saved to: {ModelPath} ({Size} bytes)", modelPath, fileInfo.Length);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving model to: {ModelPath}", modelPath);
                throw;
            }
        }
    }
}
