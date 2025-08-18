using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IOCRCacheService
    {        
        Task<EnhancedOCRResult?> GetCachedResultAsync(string imageHash);
        Task SetCachedResultAsync(string imageHash, EnhancedOCRResult result, TimeSpan? expiration = null);       
        Task RemoveCachedResultAsync(string imageHash);
        Task<string> CalculateImageHashAsync(string imagePath);       
        Task<string> CalculateImageHashAsync(Stream imageStream);        
        Task ClearCacheAsync();       
        Task<Dictionary<string, object>> GetCacheStatisticsAsync();
    }
}
