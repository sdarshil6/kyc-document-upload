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

        //[HttpGet("train-model")]
        //public async Task<ActionResult<TrainingMetrics>> TrainModel()
        //{
        //    try
        //    {
        //        return await _mlModelTrainingService.TrainModelAsync();
        //    }
        //    catch(Exception ex)
        //    {
        //        _logger.LogError("Error occured inside TrainModel() in TrainingDataController.cs : " + ex);
        //        throw;
        //    }
        //}

        [HttpGet("train-model-on-limited-types")]
        public async Task<ActionResult<TrainingMetrics>> TrainModelOnLimitedTypes()
        {
            try
            {
                return await _mlModelTrainingService.TrainModelAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside TrainModelOnLimitedTypes() in TrainingDataController.cs : " + ex);
                throw;
            }
        }        
    }
}