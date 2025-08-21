using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using KYCDocumentAPI.ML.OCR.Services;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.Services
{
    public class ProductionOCRService : IOCRService
    {
        private readonly IEnhancedOCRService _enhancedOCRService;
        private readonly ILogger<ProductionOCRService> _logger;
        public ProductionOCRService(IEnhancedOCRService enhancedOCRService, ILogger<ProductionOCRService> logger)
        {
            _enhancedOCRService = enhancedOCRService;
            _logger = logger;
        }
        public async Task<ImageQualityMetrics> AnalyzeImageQualityAsync(string imagePath)
        {
            try
            {
                return await _enhancedOCRService.AnalyzeImageQualityAsync(imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image quality for {ImagePath}", imagePath);

                return new ImageQualityMetrics
                {
                    OverallQuality = 0.5f,
                    QualityIssues = new List<string> { "Quality analysis failed" },
                    Recommendations = new List<string> { "Manual review recommended" }
                };
            }
        }

        public async Task<EnhancedOCRResult> ExtractTextFromImageAsync(string imagePath)
        {
            try
            {
                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "en", "hi" },
                    PreferredEngine = OCREngine.Tesseract,
                    EnableFallback = true,
                    PreprocessImage = true,
                    AnalyzeQuality = true,
                    TimeoutSeconds = 30,
                    MinimumConfidence = 0.5f
                };
                return await _enhancedOCRService.ExtractTextAsync(imagePath, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OCR processing for {ImagePath}", imagePath);

                return new EnhancedOCRResult
                {
                    Success = false,
                    ExtractedText = string.Empty,
                    OverallConfidence = 0f,
                    Errors = new List<string> { ex.Message },
                    ProcessingTime = TimeSpan.Zero
                };
            }
        }

        public async Task<EnhancedOCRResult> ExtractTextFromPDFAsync(string pdfPath)
        {
            try
            {                
                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "en", "hi" },
                    PreferredEngine = OCREngine.Tesseract,
                    EnableFallback = true,
                    PreprocessImage = true,
                    AnalyzeQuality = true,
                    TimeoutSeconds = 60, // Longer timeout for PDFs
                    MinimumConfidence = 0.5f
                };
                return await _enhancedOCRService.ExtractTextAsync(pdfPath, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PDF OCR processing for {PDFPath}", pdfPath);

                return new EnhancedOCRResult
                {
                    Success = false,
                    ExtractedText = string.Empty,
                    OverallConfidence = 0f,
                    Errors = new List<string> { ex.Message },
                    ProcessingTime = TimeSpan.Zero
                };
            }
        }
    }
}
