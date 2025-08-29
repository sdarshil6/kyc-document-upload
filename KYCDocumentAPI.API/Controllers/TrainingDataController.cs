using KYCDocumentAPI.ML.Services;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.ML.Models;

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
        private readonly IMLModelTrainingService _mlModelTrainingService;

        public TrainingDataController(ITrainingDataService trainingDataService, ILogger<TrainingDataController> logger, IConfiguration configuration, IMLModelTrainingService mlModelTrainingService)
        {
            _trainingDataService = trainingDataService;
            _logger = logger;
            
            _trainingDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TrainingData", "Images");
            if (string.IsNullOrWhiteSpace(_trainingDataPath))
                throw new ApplicationException("TrainingData folder path does not exist.");

            _mlModelTrainingService = mlModelTrainingService;
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

        [HttpGet("train-model")]
        public async Task<ActionResult<TrainingMetrics>> TrainModel()
        {
            try
            {
                return await _mlModelTrainingService.TrainModelAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError("Error occured inside TrainModel() in TrainingDataController.cs : " + ex);
                throw;
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