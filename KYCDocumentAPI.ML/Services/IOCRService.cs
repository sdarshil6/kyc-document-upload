using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface IOCRService
    {
        Task<OCRResult> ExtractTextFromImageAsync(string imagePath);
        Task<OCRResult> ExtractTextFromPDFAsync(string pdfPath);
        Task<ImageQualityResult> AnalyzeImageQualityAsync(string imagePath);
    }
}
