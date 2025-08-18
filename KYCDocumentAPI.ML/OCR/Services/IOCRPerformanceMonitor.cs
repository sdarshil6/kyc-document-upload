using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IOCRPerformanceMonitor
    {        
        Task RecordProcessingMetricsAsync(OCREngine engine, TimeSpan processingTime, bool success);       
        Task<Dictionary<string, object>> GetEnginePerformanceAsync(OCREngine engine);        
        Task<Dictionary<string, object>> GetSystemPerformanceAsync();        
        Task<Dictionary<string, object>> GetPerformanceTrendsAsync(DateTime from, DateTime to);
    }
}
