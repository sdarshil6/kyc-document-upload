using KYCDocumentAPI.API.Models.DTOs;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.ML.Services;

namespace KYCDocumentAPI.API.Controllers
{
    public class AITestController : ControllerBase
    {
        private readonly IDocumentClassificationService _classificationService;
        private readonly IOCRService _ocrService;                
        private readonly ILogger<AITestController> _logger;

        public AITestController(IDocumentClassificationService classificationService, IOCRService ocrService, ILogger<AITestController> logger)
        {
            _classificationService = classificationService;
            _ocrService = ocrService;            
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

                var ext = ValidateUploadedFileAndGetExtension(request.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
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
                    Confidence = Math.Round(ocrResult.OverallConfidence * 100, 1),
                    DetectedLanguages = ocrResult.DetectedLanguages,
                    ProcessingTimeMs = ocrResult.ProcessingTime.TotalMilliseconds,
                    CharacterCount = ocrResult.ExtractedText.Length,
                    Errors = ocrResult.Errors
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "OCR processing completed"));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in AITestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
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

                var ext = ValidateUploadedFileAndGetExtension(classifyRequest.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await classifyRequest.File.CopyToAsync(stream);
                }

                // Run classification
                var classificationResult = await _classificationService.ClassifyDocumentAsync(tempPath);

                // Clean up
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    PredictedType = classificationResult.PredictedLabel,
                    Confidence = Math.Round(classificationResult.Confidence * 100, 1)                                       
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Document classification completed"));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in AITestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in classification test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Classification test failed"));
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

                var ext = ValidateUploadedFileAndGetExtension(qualityRequest.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
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
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in AITestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quality test");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Quality test failed"));
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
                _logger.LogError("Error occured inside ValidateUploadedFileAndGetExtension() in AITestController.cs : " + ex);
                throw;
            }
        }
    }
}
