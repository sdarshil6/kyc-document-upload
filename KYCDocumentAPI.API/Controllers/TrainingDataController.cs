using KYCDocumentAPI.ML.Services;
using KYCDocumentAPI.API.Models.Responses;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TrainingDataController : ControllerBase
    {
        private readonly ITrainingDataService _trainingDataService;
        private readonly ILogger<TrainingDataController> _logger;
        private readonly string _trainingDataPath;

        public TrainingDataController(
            ITrainingDataService trainingDataService,
            ILogger<TrainingDataController> logger,
            IConfiguration configuration)
        {
            _trainingDataService = trainingDataService;
            _logger = logger;
            
            _trainingDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TrainingData", "Images");
            if (string.IsNullOrWhiteSpace(_trainingDataPath))
                throw new ApplicationException("TrainingData folder path does not exist.");
        }

        [HttpGet("analyze")]
        public async Task<ActionResult<ApiResponse<object>>> AnalyzeTrainingData()
        {
            try
            {
                var stats = await _trainingDataService.AnalyzeDatasetAsync(_trainingDataPath);

                var response = new
                {
                    DatasetPath = _trainingDataPath,
                    Analysis = new
                    {
                        TotalImages = stats.TotalImages,
                        NumberOfClasses = stats.ImagesPerClass.Count,                      
                        HasSufficientData = stats.HasSufficientData,
                        InvalidImagesCount = stats.InvalidImages.Count,
                        MissingClassesCount = stats.MissingClasses.Count
                    },
                    ClassDistribution = stats.ImagesPerClass.ToDictionary(
                        x => x.Key,
                        x => new
                        {
                            ImageCount = x.Value,
                            Status = x.Value >= 50 ? "Excellent" :
                                    x.Value >= 20 ? "Good" :
                                    x.Value >= 10 ? "Minimum" :
                                    x.Value > 0 ? "Insufficient" : "Missing"
                        }
                    ),
                    QualityAssessment = new
                    {
                        ReadyForTraining = stats.HasSufficientData && !stats.InvalidImages.Any(),
                        MissingClasses = stats.MissingClasses,
                        InvalidImages = stats.InvalidImages.ToList(),
                        TotalInvalidImages = stats.InvalidImages.Count
                    },
                    Recommendations = stats.Recommendations,
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Training data analysis completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing training data");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Training data analysis failed"));
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<object>>> ValidateTrainingData()
        {
            try
            {
                _logger.LogInformation("Starting training data validation");

                var allImages = await _trainingDataService.LoadTrainingDataAsync(_trainingDataPath);
                var validationResults = new List<object>();

                foreach (var imageData in allImages)
                {
                    var isValid = await _trainingDataService.ValidateImageAsync(imageData.ImagePath);

                    validationResults.Add(new
                    {
                        ImagePath = imageData.ImagePath,
                        FileName = imageData.OriginalFileName,
                        Label = imageData.Label,
                        IsValid = isValid,
                        FileSizeKB = Math.Round(imageData.FileSize / 1024.0, 1),
                        ValidationStatus = isValid ? "Valid" : "Invalid"
                    });
                }

                var validCount = validationResults.Count(r => (bool)((dynamic)r).IsValid);
                var invalidCount = validationResults.Count - validCount;

                var response = new
                {
                    Summary = new
                    {
                        TotalImages = validationResults.Count,
                        ValidImages = validCount,
                        InvalidImages = invalidCount,                        
                    },
                    ValidImages = validationResults.Where(r => (bool)((dynamic)r).IsValid).ToList(),
                    InvalidImages = validationResults.Where(r => !(bool)((dynamic)r).IsValid).ToList(),
                    ClassBreakdown = validationResults.GroupBy(r => ((dynamic)r).Label).ToDictionary(g => g.Key, g => new
                    {
                        Total = g.Count(),
                        Valid = g.Count(r => (bool)((dynamic)r).IsValid),
                        Invalid = g.Count(r => !(bool)((dynamic)r).IsValid)
                    })
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Training data validation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating training data");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Training data validation failed"));
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<object>>> GetTrainingDataStatus()
        {
            try
            {
                var folderExists = Directory.Exists(_trainingDataPath);
                var stats = folderExists ? await _trainingDataService.AnalyzeDatasetAsync(_trainingDataPath) : null;

                var status = new
                {
                    TrainingDataPath = _trainingDataPath,
                    FolderStructure = new
                    {
                        BasePathExists = folderExists,
                        RequiredFolders = CheckRequiredFolders()                       
                    },
                    DatasetStatus = stats != null ? new
                    {
                        TotalImages = stats.TotalImages,
                        ReadyForTraining = stats.HasSufficientData,
                        IssueCount = stats.Recommendations.Count,
                        Suggestions = stats.Recommendations
                    } : null,
                    Requirements = new
                    {
                        MinimumImagesPerClass = 20,
                        RecommendedImagesPerClass = 50,
                        TotalMinimumImages = 180,
                        TotalRecommendedImages = 450,
                        SupportedFormats = new[] { ".jpg", ".jpeg", ".png" },
                        MaxFileSize = "10MB",
                        MinImageDimensions = "100x100 pixels",
                        MaxImageDimensions = "4000x4000 pixels"
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(status, "Training data status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training data status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get training data status"));
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDetailedStatistics()
        {
            try
            {
                var allImages = await _trainingDataService.LoadTrainingDataAsync(_trainingDataPath);

                if (allImages.Count == 0)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { Message = "No training data found" }, "No data to analyze"));
                }

                var classStatistics = allImages
                    .GroupBy(x => x.Label)
                    .ToDictionary(g => g.Key, g => new
                    {
                        ImageCount = g.Count(),
                        TotalSizeMB = Math.Round(g.Sum(x => x.FileSize) / (1024.0 * 1024.0), 2),
                        AverageFileSizeKB = Math.Round(g.Average(x => x.FileSize) / 1024.0, 1),
                        ImageFiles = g.Select(x => new
                        {
                            FileName = x.OriginalFileName,
                            SizeKB = Math.Round(x.FileSize / 1024.0, 1),
                            Path = x.ImagePath,
                            IsAugmented = x.IsAugmented
                        }).OrderBy(x => x.FileName).ToList()
                    });

                var overallStats = new
                {
                    TotalImages = allImages.Count,
                    TotalSizeMB = Math.Round(allImages.Sum(x => x.FileSize) / (1024.0 * 1024.0), 2),
                    AverageImagesPerClass = Math.Round((double)allImages.Count / classStatistics.Count, 1),
                    DocumentTypes = classStatistics.Count,
                    OldestImage = allImages.Min(x => x.CreatedDate),
                    NewestImage = allImages.Max(x => x.CreatedDate),
                    AugmentedImages = allImages.Count(x => x.IsAugmented)
                };

                var response = new
                {
                    OverallStatistics = overallStats,
                    ClassStatistics = classStatistics,
                    DataQuality = new
                    {
                        HasSufficientData = allImages.Count >= 180,
                        LargestClass = classStatistics.OrderByDescending(x => x.Value.ImageCount).First().Key,
                        SmallestClass = classStatistics.OrderBy(x => x.Value.ImageCount).First().Key,
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Detailed statistics retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed statistics");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get statistics"));
            }
        }
       
        private Dictionary<string, bool> CheckRequiredFolders()
        {
            try
            {
                var documentTypes = new[] { "Aadhaar", "PAN", "Passport", "DrivingLicense", "VoterID" };

                return documentTypes.ToDictionary(
                    docType => docType,
                    docType => Directory.Exists(Path.Combine(_trainingDataPath, docType))
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside CheckRequiredFolders() in TrainingDataController.cs : " + ex);
                throw;
            }
        }        
    }
}