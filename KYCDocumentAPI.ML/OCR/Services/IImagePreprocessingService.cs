using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IImagePreprocessingService
    {        
        Task<string> PreprocessImageAsync(string imagePath, ImagePreprocessingOptions options);      
        Task<Stream> PreprocessImageAsync(Stream imageStream, ImagePreprocessingOptions options);       
        Task<List<string>> RecommendPreprocessingStepsAsync(string imagePath);       
        Task<string> CorrectOrientationAsync(string imagePath);        
        Task<string> EnhanceQualityAsync(string imagePath, ImagePreprocessingOptions options);
    }
}
