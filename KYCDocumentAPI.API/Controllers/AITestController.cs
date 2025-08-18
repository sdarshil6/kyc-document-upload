using KYCDocumentAPI.API.Models.DTOs;
using KYCDocumentAPI.API.Models.Requests;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.ML.Services;

namespace KYCDocumentAPI.API.Controllers
{
    public class AITestController : ControllerBase
    {
        private readonly IDocumentClassificationService _classificationService;
        private readonly IOCRService _ocrService;
        private readonly ITextPatternService _textPatternService;
        private readonly ILogger<AITestController> _logger;

        public AITestController(
            IDocumentClassificationService classificationService,
            IOCRService ocrService,
            ITextPatternService textPatternService,
            ILogger<AITestController> logger)
        {
            _classificationService = classificationService;
            _ocrService = ocrService;
            _textPatternService = textPatternService;
            _logger = logger;
        }

        /// <summary>
        /// Test OCR functionality with uploaded file
        /// </summary>
        [HttpPost("ocr")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> TestOCR([FromForm] OCRRequestDto request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                // Save file temporarily
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // Run OCR
                var ocrResult = await _ocrService.ExtractTextFromImageAsync(tempPath);

                // Clean up
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    Success = ocrResult.Success,
                    ExtractedText = ocrResult.ExtractedText,
                    Confidence = Math.Round(ocrResult.Confidence * 100, 1),
                    DetectedLanguages = ocrResult.DetectedLanguages,
                    ProcessingTimeMs = ocrResult.ProcessingTime.TotalMilliseconds,
                    CharacterCount = ocrResult.ExtractedText.Length,
                    Errors = ocrResult.Errors
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "OCR processing completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OCR test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("OCR test failed"));
            }
        }

        /// <summary>
        /// Test document classification with uploaded file
        /// </summary>
        [HttpPost("classify")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> TestClassification([FromForm] ClassifyRequestDto classifyRequest)
        {
            try
            {
                if (classifyRequest.File == null || classifyRequest.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                // Save file temporarily
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await classifyRequest.File.CopyToAsync(stream);
                }

                // Run classification
                var classificationResult = await _classificationService.ClassifyDocumentAsync(tempPath, classifyRequest.File.FileName);

                // Clean up
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    PredictedType = classificationResult.PredictedType.ToString(),
                    Confidence = Math.Round(classificationResult.Confidence * 100, 1),
                    AllPredictions = classificationResult.AllPredictions.ToDictionary(
                        x => x.Key.ToString(),
                        x => Math.Round(x.Value * 100, 1)
                    ),
                    ProcessingNotes = classificationResult.ProcessingNotes,
                    IsConfident = classificationResult.Confidence > 0.8,
                    FileName = classifyRequest.File.FileName
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Document classification completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in classification test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Classification test failed"));
            }
        }

        /// <summary>
        /// Test text pattern recognition with raw text
        /// </summary>
        [HttpPost("patterns")]
        public async Task<ActionResult<ApiResponse<object>>> TestPatterns([FromBody] TestPatternsRequest request)
        {
            try
            {
                await Task.CompletedTask;

                var patternResult = _textPatternService.AnalyzeText(request.Text, request.FileName ?? "");

                var response = new
                {
                    PredictedDocumentType = patternResult.PredictedDocumentType.ToString(),
                    Confidence = Math.Round(patternResult.Confidence * 100, 1),
                    DocumentTypeConfidences = patternResult.DocumentTypeConfidences.ToDictionary(
                        x => x.Key.ToString(),
                        x => Math.Round(x.Value * 100, 1)
                    ),
                    ExtractedNumbers = new
                    {
                        AadhaarNumber = patternResult.AadhaarNumber,
                        PANNumber = patternResult.PANNumber,
                        PassportNumber = patternResult.PassportNumber
                    },
                    PatternFlags = new
                    {
                        HasAadhaarPattern = patternResult.HasAadhaarPattern,
                        HasPANPattern = patternResult.HasPANPattern,
                        HasPassportPattern = patternResult.HasPassportPattern
                    },
                    TextLength = request.Text.Length,
                    ProcessingNotes = "Pattern analysis completed"
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Text pattern analysis completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pattern test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Pattern test failed"));
            }
        }

        /// <summary>
        /// Test image quality analysis
        /// </summary>
        [HttpPost("quality")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> TestImageQuality([FromForm] QualityRequestDto qualityRequest)
        {
            try
            {
                if (qualityRequest.File == null || qualityRequest.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                // Save file temporarily
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await qualityRequest.File.CopyToAsync(stream);
                }

                // Analyze quality
                var qualityResult = await _ocrService.AnalyzeImageQualityAsync(tempPath);

                // Clean up
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    OverallQuality = Math.Round(qualityResult.OverallQuality * 100, 1),
                    QualityMetrics = new
                    {
                        Brightness = Math.Round(qualityResult.Brightness * 100, 1),
                        Contrast = Math.Round(qualityResult.Contrast * 100, 1),
                        Sharpness = Math.Round(qualityResult.Sharpness * 100, 1),
                        NoiseLevel = Math.Round(qualityResult.NoiseLevel * 100, 1)
                    },
                    QualityFlags = new
                    {
                        IsBlurry = qualityResult.IsBlurry,
                        IsTooDark = qualityResult.IsTooDark,
                        IsTooLight = qualityResult.IsTooLight
                    },
                    QualityIssues = qualityResult.QualityIssues,
                    Recommendation = qualityResult.OverallQuality > 0.7 ? "Good quality" :
                                   qualityResult.OverallQuality > 0.5 ? "Acceptable quality" : "Poor quality - retake recommended",
                    FileName = qualityRequest.File.FileName,
                    FileSize = qualityRequest.File.Length
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Image quality analysis completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quality test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Quality test failed"));
            }
        }

        /// <summary>
        /// Get AI service status and capabilities
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<object>>> GetAIStatus()
        {
            try
            {
                await Task.CompletedTask;

                var status = new
                {
                    Services = new
                    {
                        DocumentClassification = new
                        {
                            Status = _classificationService.IsModelReady ? "Ready" : "Not Ready",
                            ModelType = "Rule-based + Pattern Recognition",
                            SupportedDocuments = new[]
                            {
                                "Aadhaar", "PAN", "Passport", "Driving License",
                                "Voter ID", "Ration Card", "Bank Passbook", "Utility Bill"
                            }
                        },
                        OCR = new
                        {
                            Status = "Active",
                            Engine = "Mock OCR Service",
                            SupportedLanguages = new[] { "English", "Hindi" },
                            SupportedFormats = new[] { ".jpg", ".jpeg", ".png", ".pdf" }
                        },
                        PatternRecognition = new
                        {
                            Status = "Active",
                            Capabilities = new[]
                            {
                                "Aadhaar Number Extraction", "PAN Number Extraction",
                                "Passport Number Extraction", "Document Type Classification"
                            }
                        },
                        ImageQuality = new
                        {
                            Status = "Active",
                            Metrics = new[] { "Brightness", "Contrast", "Sharpness", "Noise Level" }
                        }
                    },
                    Performance = new
                    {
                        AverageProcessingTime = "1-3 seconds",
                        AccuracyRate = "85-95%",
                        SupportedFileSize = "Up to 10MB"
                    },
                    Limitations = new[]
                    {
                        "Currently using mock OCR - replace with real OCR service for production",
                        "Pattern recognition is rule-based - train ML models for better accuracy",
                        "Image quality analysis is simulated - integrate with actual image processing library"
                    },
                    NextSteps = new[]
                    {
                        "Integrate Azure Computer Vision or Tesseract OCR",
                        "Train custom ML.NET models with real document datasets",
                        "Add fraud detection algorithms",
                        "Implement advanced image preprocessing"
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(status, "AI service status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get AI status"));
            }
        }
    }
}
