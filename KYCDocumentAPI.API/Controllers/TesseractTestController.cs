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

        /// <summary>
        /// Test Tesseract OCR engine directly
        /// </summary>
        [HttpPost("tesseract-direct")]
        public async Task<ActionResult<ApiResponse<object>>> TestTesseractDirect([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));
               
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                var tesseractEngine = _engineFactory.CreateEngine(OCREngine.Tesseract);

                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "eng", "hin" },
                    PreprocessImage = true,
                    ExtractWordDetails = true,
                    MinimumConfidence = 0.5f
                };

                var result = await tesseractEngine.ExtractTextAsync(tempPath, options);
                
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    Engine = "Tesseract",
                    Success = result.Success,
                    ExtractedText = result.ExtractedText,
                    Confidence = Math.Round(result.Confidence * 100, 1),
                    ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                    WordCount = result.WordDetails.Count,
                    WordDetails = result.WordDetails.Take(10).Select(w => new
                    {
                        Text = w.Text,
                        Confidence = Math.Round(w.Confidence * 100, 1),
                        IsNumeric = w.IsNumeric,
                        Language = w.Language,
                        BoundingBox = new { w.BoundingBox.X, w.BoundingBox.Y, w.BoundingBox.Width, w.BoundingBox.Height }
                    }),
                    EngineData = result.EngineSpecificData,
                    ErrorMessage = result.ErrorMessage
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Tesseract OCR test completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Tesseract direct test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Tesseract test failed"));
            }
        }

        /// <summary>
        /// Test Enhanced OCR service with hybrid approach
        /// </summary>
        [HttpPost("enhanced-ocr")]
        public async Task<ActionResult<ApiResponse<object>>> TestEnhancedOCR([FromForm] IFormFile file, [FromForm] string documentType = "unknown")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                // Save file temporarily
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "en", "hi" },
                    PreferredEngine = OCREngine.Tesseract,
                    EnableFallback = true,
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

                return Ok(ApiResponse<object>.SuccessResponse(response, "Enhanced OCR test completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced OCR test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Enhanced OCR test failed"));
            }
        }

        /// <summary>
        /// Get Tesseract engine status and capabilities
        /// </summary>
        [HttpGet("tesseract-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetTesseractStatus()
        {
            try
            {
                var tesseractEngine = _engineFactory.CreateEngine(OCREngine.Tesseract);

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

        /// <summary>
        /// Get all OCR engines status
        /// </summary>
        [HttpGet("all-engines-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllEnginesStatus()
        {
            try
            {
                var engineStatuses = await _enhancedOCRService.GetEngineStatusAsync();
                var engineCapabilities = await _enhancedOCRService.GetEngineCapabilitiesAsync();
                var performanceMetrics = await _enhancedOCRService.GetPerformanceMetricsAsync();

                var response = new
                {
                    Summary = new
                    {
                        TotalEngines = engineStatuses.Count,
                        HealthyEngines = engineStatuses.Count(s => s.IsHealthy),
                        AvailableEngines = engineStatuses.Count(s => s.IsAvailable)
                    },
                    EngineStatuses = engineStatuses.Select(s => new
                    {
                        Engine = s.Engine.ToString(),
                        IsAvailable = s.IsAvailable,
                        IsHealthy = s.IsHealthy,
                        StatusMessage = s.StatusMessage,
                        SuccessRate = Math.Round(s.SuccessRate * 100, 1),
                        LastHealthCheck = s.LastHealthCheck
                    }),
                    EngineCapabilities = engineCapabilities.Select(c => new
                    {
                        Engine = c.Engine.ToString(),
                        Version = c.Version,
                        SupportedLanguages = c.SupportedLanguages.Take(5), // Show first 5 languages
                        AverageAccuracy = Math.Round(c.AverageAccuracy * 100, 1),
                        Features = new
                        {
                            WordDetails = c.SupportsWordDetails,
                            MultipleLanguages = c.SupportsMultipleLanguages,
                            Handwriting = c.SupportsHandwriting
                        }
                    }),
                    PerformanceMetrics = performanceMetrics,
                    Recommendations = GenerateRecommendations(engineStatuses)
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "All engines status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all engines status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get engines status"));
            }
        }

        /// <summary>
        /// Test text extraction from sample text image
        /// </summary>
        [HttpPost("test-sample")]
        public async Task<ActionResult<ApiResponse<object>>> TestWithSampleText([FromForm] string sampleText = "Test Document\nSample Text for OCR\n123456789")
        {
            try
            {                
                var testImagePath = await CreateTestImageWithTextAsync(sampleText);

                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "eng" },
                    PreferredEngine = OCREngine.Tesseract,
                    PreprocessImage = false,
                    ExtractWordDetails = true
                };

                var result = await _enhancedOCRService.ExtractTextAsync(testImagePath, options);

                // Cleanup
                System.IO.File.Delete(testImagePath);

                var response = new
                {
                    Input = new
                    {
                        OriginalText = sampleText,
                        CharacterCount = sampleText.Length
                    },
                    Output = new
                    {
                        ExtractedText = result.ExtractedText,
                        Success = result.Success,
                        Confidence = Math.Round(result.OverallConfidence * 100, 1),
                        ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds
                    },
                    Accuracy = new
                    {
                        TextMatch = CalculateTextSimilarity(sampleText, result.ExtractedText),
                        CharacterAccuracy = CalculateCharacterAccuracy(sampleText, result.ExtractedText)
                    },
                    EngineUsed = result.PrimaryEngine.ToString(),
                    UsedFallback = result.ProcessingStats.UsedFallback
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Sample text OCR test completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sample text test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Sample text test failed"));
            }
        }

        private async Task<string> CreateTestImageWithTextAsync(string text)
        {
            var testImagePath = Path.GetTempFileName() + ".png";

            try
            {
                using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(400, 200);

                image.Mutate(x => x
                    .Fill(Color.White)
                    .DrawText(text,
                        SystemFonts.CreateFont("Arial", 16),
                        Color.Black,
                        new PointF(20, 20)));

                await image.SaveAsync(testImagePath);
                return testImagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create test image");
                throw;
            }
        }

        private List<string> GenerateRecommendations(List<OCREngineStatus> statuses)
        {
            var recommendations = new List<string>();

            var healthyEngines = statuses.Count(s => s.IsHealthy);
            var totalEngines = statuses.Count;

            if (healthyEngines == 0)
            {
                recommendations.Add("⚠️ No OCR engines are healthy. Check system configuration.");
            }
            else if (healthyEngines == 1)
            {
                recommendations.Add("⚠️ Only one OCR engine is healthy. Consider fixing other engines for better reliability.");
            }
            else
            {
                recommendations.Add("✅ Multiple OCR engines are healthy. Good redundancy for production use.");
            }

            if (statuses.Any(s => s.SuccessRate < 0.9f && s.SuccessfulRequests > 0))
            {
                recommendations.Add("⚠️ Some engines have low success rates. Monitor and investigate failures.");
            }

            return recommendations;
        }

        private double CalculateTextSimilarity(string original, string extracted)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(extracted))
                return 0.0;

            var originalWords = original.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var extractedWords = extracted.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            var matchingWords = originalWords.Intersect(extractedWords, StringComparer.OrdinalIgnoreCase).Count();
            return originalWords.Length > 0 ? (double)matchingWords / originalWords.Length : 0.0;
        }

        private double CalculateCharacterAccuracy(string original, string extracted)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(extracted))
                return 0.0;

            var originalChars = original.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray();
            var extractedChars = extracted.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray();

            var minLength = Math.Min(originalChars.Length, extractedChars.Length);
            var matchingChars = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (originalChars[i] == extractedChars[i])
                    matchingChars++;
            }

            return originalChars.Length > 0 ? (double)matchingChars / originalChars.Length : 0.0;
        }
    }
}
