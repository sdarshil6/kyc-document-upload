using KYCDocumentAPI.ML.Models;
using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IOCRService
    {
        Task<EnhancedOCRResult> ExtractTextFromImageAsync(string imagePath);
        Task<EnhancedOCRResult> ExtractTextFromPDFAsync(string pdfPath);
        Task<ImageQualityMetrics> AnalyzeImageQualityAsync(string imagePath);
    }
}
