using KYCDocumentAPI.API.Models.Requests;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using KYCDocumentAPI.ML.OCR.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TesseractTestController : ControllerBase
    {
        private readonly IEnhancedOCRService _enhancedOCRService;
        private readonly IOCREngineFactory _engineFactory;
        private readonly ILogger<TesseractTestController> _logger;

        public TesseractTestController(IEnhancedOCRService enhancedOCRService, IOCREngineFactory engineFactory, ILogger<TesseractTestController> logger)
        {
            _enhancedOCRService = enhancedOCRService;
            _engineFactory = engineFactory;
            _logger = logger;
        }        
        
        [HttpPost("tesseract-direct")]
        public async Task<ActionResult<ApiResponse<object>>> TestTesseractDirect([FromForm] TestTesseractDirectRequest req)
        {
            try
            {
                if (req.File == null || req.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                var ext = ValidateUploadedFileAndGetExtension(req.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await req.File.CopyToAsync(stream);
                }
                
                var options = new OCRProcessingOptions
                {                    
                    PreferredEngine = OCREngine.Tesseract,                    
                    PreprocessImage = true,
                    AnalyzeQuality = true,
                    ExtractWordDetails = true,
                    MinimumConfidence = 0.5f
                };

                var result = await _enhancedOCRService.ExtractTextAsync(tempPath, options);
                
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    OverallResult = new
                    {
                        Success = result.Success,
                        ExtractedText = result.ExtractedText,
                        OverallConfidence = Math.Round(result.OverallConfidence * 100, 1),
                        ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                        DetectedLanguages = result.DetectedLanguages
                    },
                    QualityAnalysis = result.QualityMetrics != null ? new
                    {
                        OverallQuality = Math.Round(result.QualityMetrics.OverallQuality * 100, 1),
                        Brightness = Math.Round(result.QualityMetrics.Brightness * 100, 1),
                        Contrast = Math.Round(result.QualityMetrics.Contrast * 100, 1),
                        Sharpness = Math.Round(result.QualityMetrics.Sharpness * 100, 1),
                        QualityIssues = result.QualityMetrics.QualityIssues,
                        Recommendations = result.QualityMetrics.Recommendations
                    } : null,
                    EngineResults = result.EngineResults.Select(er => new
                    {
                        Engine = er.Engine.ToString(),
                        Success = er.Success,
                        Confidence = Math.Round(er.Confidence * 100, 1),
                        ProcessingTimeMs = er.ProcessingTime.TotalMilliseconds,
                        TextLength = er.ExtractedText.Length,
                        WordCount = er.WordDetails.Count,
                        ErrorMessage = er.ErrorMessage
                    }),
                    TextAnalysis = result.TextAnalysis != null ? new
                    {
                        TotalCharacters = result.TextAnalysis.TotalCharacters,
                        TotalWords = result.TextAnalysis.TotalWords,
                        TotalLines = result.TextAnalysis.TotalLines,
                        AverageWordConfidence = Math.Round(result.TextAnalysis.AverageWordConfidence * 100, 1),
                        DetectedPatterns = result.TextAnalysis.DetectedPatterns,
                        Complexity = result.TextAnalysis.Complexity.ToString(),
                        HasNumbers = result.TextAnalysis.HasNumbers,
                        HasDates = result.TextAnalysis.HasDates
                    } : null,
                    ProcessingStats = new
                    {
                        PrimaryEngine = result.PrimaryEngine.ToString(),
                        FallbackEngine = result.FallbackEngine?.ToString(),
                        UsedFallback = result.ProcessingStats.UsedFallback,
                        PrimaryEngineTimeMs = result.ProcessingStats.PrimaryEngineTime.TotalMilliseconds,
                        FallbackEngineTimeMs = result.ProcessingStats.FallbackEngineTime.TotalMilliseconds
                    },
                    Metadata = result.Metadata
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "OCR Successful."));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in TesseractTestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced OCR test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("OCR Failed."));
            }
        }
        
        [HttpGet("tesseract-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetTesseractStatus()
        {
            try
            {
                var tesseractEngine = _engineFactory.CreateEngine();

                var status = await tesseractEngine.GetStatusAsync();
                var capabilities = await tesseractEngine.GetCapabilitiesAsync();
                var healthCheck = await tesseractEngine.HealthCheckAsync();

                var response = new
                {
                    Status = new
                    {
                        Engine = status.Engine.ToString(),
                        IsAvailable = status.IsAvailable,
                        IsHealthy = status.IsHealthy,
                        StatusMessage = status.StatusMessage,
                        SuccessfulRequests = status.SuccessfulRequests,
                        FailedRequests = status.FailedRequests,
                        SuccessRate = Math.Round(status.SuccessRate * 100, 1),
                        AverageResponseTimeMs = status.AverageResponseTime.TotalMilliseconds,
                        LastHealthCheck = status.LastHealthCheck
                    },
                    Capabilities = new
                    {
                        Engine = capabilities.Engine.ToString(),
                        Version = capabilities.Version,
                        SupportedLanguages = capabilities.SupportedLanguages,
                        SupportedFormats = capabilities.SupportedFormats,
                        Features = new
                        {
                            WordDetails = capabilities.SupportsWordDetails,
                            ConfidenceScores = capabilities.SupportsConfidenceScores,
                            MultipleLanguages = capabilities.SupportsMultipleLanguages,
                            Handwriting = capabilities.SupportsHandwriting
                        },
                        Performance = new
                        {
                            AverageAccuracy = Math.Round(capabilities.AverageAccuracy * 100, 1),
                            AverageSpeed = capabilities.AverageSpeed
                        },
                        AdditionalCapabilities = capabilities.AdditionalCapabilities
                    },
                    HealthCheck = new
                    {
                        Passed = healthCheck,
                        Timestamp = DateTime.UtcNow
                    },
                    SystemInfo = new
                    {
                        OperatingSystem = Environment.OSVersion.ToString(),
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSetMB = GC.GetTotalMemory(false) / (1024 * 1024)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Tesseract status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Tesseract status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get Tesseract status"));
            }
        }         

        private string ValidateUploadedFileAndGetExtension(string fileName)
        {
            try
            {
                var ext = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(ext))
                    throw new NotSupportedException("No image extension found. Invalid file uploaded. Kindly upload valid image.");
                else if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("PDF files are not supported. Please upload an image file (PNG,JPG,JPEG,TIFF).");
                return ext;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside ValidateUploadedFileAndGetExtension() in TesseractTestController.cs : " + ex);
                throw;
            }
        }
    }
}
