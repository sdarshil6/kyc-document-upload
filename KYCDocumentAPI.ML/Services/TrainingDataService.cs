using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.ML.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace KYCDocumentAPI.ML.Services
{
    public class TrainingDataService : ITrainingDataService
    {
        private readonly ILogger<TrainingDataService> _logger;
        private readonly string[] _validImageExtensions = { ".jpg", ".jpeg", ".png"};
        private readonly string[] _documentTypes = { "Aadhaar Front", "Aadhaar Back", "Aadhaar Regular", "PAN", "Passport", "Driving License", "Voter Id" };

        public TrainingDataService(ILogger<TrainingDataService> logger)
        {
            _logger = logger;
        }        

        public async Task<List<ImageData>> LoadTrainingDataAsync(string dataPath)
        {
            try
            {
                if (!Directory.Exists(dataPath))
                    throw new DirectoryNotFoundException($"Training data directory not found: {dataPath}");

                var trainingData = new List<ImageData>();
                _logger.LogInformation("Loading training data from: {DataPath}", dataPath);

                foreach (var documentType in _documentTypes)
                {
                    var documentFolder = Path.Combine(dataPath, documentType);

                    if (!Directory.Exists(documentFolder))
                    {
                        _logger.LogWarning("Document type folder not found: {DocumentType}", documentType);
                        continue;
                    }

                    var images = await LoadImagesFromFolderAsync(documentFolder, documentType);
                    trainingData.AddRange(images);

                    _logger.LogInformation("Found {Count} valid images for {DocumentType}", images.Count, documentType);
                }

                _logger.LogInformation("Successfully loaded {Count} training images across {ClassCount} document types", trainingData.Count, trainingData.Select(x => x.Label).Distinct().Count());

                return trainingData;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside LoadTrainingDataAsync() in TrainingDataService.cs : " + ex);
                throw;
            }
        }

        private async Task<List<ImageData>> LoadImagesFromFolderAsync(string folderPath, string documentType)
        {
            try
            {
                var imageFiles = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(file => _validImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

                var tasks = imageFiles.Select(async imageFile =>
                {
                    if (await ValidateImageAsync(imageFile))
                    {
                        var fileInfo = new FileInfo(imageFile);
                        return new ImageData
                        {
                            ImagePath = imageFile,
                            Label = documentType,
                            FileSize = fileInfo.Length,
                            OriginalFileName = fileInfo.Name,
                            CreatedDate = fileInfo.CreationTime
                        };
                    }
                    else
                    {
                        _logger.LogWarning("Invalid image skipped: {ImageFile}", imageFile);
                        return null;
                    }
                });

                var results = await Task.WhenAll(tasks);
                return [.. results.Where(x => x != null)];
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside LoadImagesFromFolderAsync() in TrainingDataService.cs : " + ex);
                throw;
            }
        }


        private async Task<bool> ValidateImageAsync(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    _logger.LogWarning("Image file does not exist: {ImagePath}", imagePath);
                    return false;
                }

                var fileInfo = new FileInfo(imagePath);

                // Check file size (minimum 10KB, maximum 10MB)
                if (fileInfo.Length < 10_000 || fileInfo.Length > 10_000_000)
                {
                    _logger.LogWarning("Image file size out of range: {Size} bytes for {ImagePath}",
                        fileInfo.Length, imagePath);
                    return false;
                }

                // Validate image format and dimensions
                using var image = await Image.LoadAsync(imagePath);

                // Check minimum dimensions (at least 100x100)
                if (image.Width < 100 || image.Height < 100)
                {
                    _logger.LogWarning("Image dimensions too small: {Width}x{Height} for {ImagePath}",
                        image.Width, image.Height, imagePath);
                    return false;
                }

                // Check maximum dimensions (prevent memory issues)
                if (image.Width > 4000 || image.Height > 4000)
                {
                    _logger.LogWarning("Image dimensions too large: {Width}x{Height} for {ImagePath}",
                        image.Width, image.Height, imagePath);
                    return false;
                }

                // Check aspect ratio (not too extreme)
                var aspectRatio = (float)image.Width / image.Height;
                if (aspectRatio < 0.3f || aspectRatio > 3.0f)
                {
                    _logger.LogWarning("Extreme aspect ratio: {AspectRatio} for {ImagePath}",
                        aspectRatio, imagePath);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating image: {ImagePath}", imagePath);
                return false;
            }
        }

        public async Task<TrainingDataStats> AnalyzeDatasetAsync(string dataPath)
        {
            var stats = new TrainingDataStats();

            try
            {
                _logger.LogInformation("Analyzing training dataset at: {DataPath}", dataPath);

                var allImages = await LoadTrainingDataAsync(dataPath);
                stats.TotalImages = allImages.Count;

                // Group by document type
                var groupedImages = allImages.GroupBy(x => x.Label).ToList();

                foreach (var group in groupedImages)
                {
                    stats.ImagesPerClass[group.Key] = group.Count();
                    stats.ImagePaths[group.Key] = group.Select(x => x.ImagePath).ToList();
                }

                // Check for missing classes
                foreach (var documentType in _documentTypes)
                {
                    if (!stats.ImagesPerClass.ContainsKey(documentType) || stats.ImagesPerClass[documentType] == 0)
                    {
                        stats.MissingClasses.Add(documentType);
                    }
                }

                // Validate individual images and collect invalid ones
                foreach (var imageData in allImages)
                {
                    if (!await ValidateImageAsync(imageData.ImagePath))
                    {
                        stats.InvalidImages.Add(imageData.ImagePath);
                    }
                }

                // Check dataset balance
                if (stats.ImagesPerClass.Count > 1)
                {
                    var counts = stats.ImagesPerClass.Values.Where(x => x > 0).ToList();
                    if (counts.Any())
                    {
                        var min = counts.Min();
                        var max = counts.Max();                        
                    }
                }
                
                var minImagesPerClass = 20;
                var recommendedImagesPerClass = 50;

                if (allImages?.Count == 0)
                    stats.HasSufficientData = false;
                else
                    stats.HasSufficientData = stats.ImagesPerClass.Values.All(count => count > minImagesPerClass);
                
                GenerateDatasetRecommendations(stats, minImagesPerClass, recommendedImagesPerClass);

                _logger.LogInformation("Dataset analysis completed: {Summary}", stats.GetSummary());

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dataset");
                throw;
            }
        }

        private void GenerateDatasetRecommendations(TrainingDataStats stats, int minimumPerClass, int recommendedPerClass)
        {
            try
            {
                stats.Recommendations.Clear();

                if (stats.MissingClasses.Any())
                {
                    stats.Recommendations.Add($"Missing document types: {string.Join(", ", stats.MissingClasses)}");
                }

                var insufficientClasses = stats.ImagesPerClass
                    .Where(x => x.Value < minimumPerClass && x.Value > 0)
                    .ToList();

                if (insufficientClasses.Any())
                {
                    var classNames = string.Join(", ", insufficientClasses.Select(x => $"{x.Key} ({x.Value})"));
                    stats.Recommendations.Add($"Need more images for: {classNames}");
                }

                var needMoreData = stats.ImagesPerClass
                    .Where(x => x.Value >= minimumPerClass && x.Value < recommendedPerClass)
                    .ToList();

                if (needMoreData.Any())
                {
                    var classNames = string.Join(", ", needMoreData.Select(x => x.Key));
                    stats.Recommendations.Add($"Could benefit from more images: {classNames}");
                }

                if (stats.InvalidImages.Any())
                {
                    stats.Recommendations.Add($"Remove or fix {stats.InvalidImages.Count} invalid images");
                }

                if (stats.TotalImages < 200)
                {
                    stats.Recommendations.Add("Collect more training data. Aim for 500+ total images for production quality.");
                }

                if (stats.TotalImages >= 200 && !stats.InvalidImages.Any())
                {
                    stats.Recommendations.Add("Dataset looks good for training! Consider data augmentation for even better results.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured inside GenerateDatasetRecommendations() in TrainingDataService.cs : " + e);
                throw;
            }
        }
    }
}
