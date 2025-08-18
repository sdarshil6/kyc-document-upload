using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.OCR.Services
{    
    public interface IOCREngine
    {        
        OCREngine EngineType { get; }        
        Task<EngineResult> ExtractTextAsync(string imagePath, OCRProcessingOptions options);        
        Task<EngineResult> ExtractTextAsync(Stream imageStream, OCRProcessingOptions options);        
        Task<OCREngineStatus> GetStatusAsync();       
        Task<OCREngineCapabilities> GetCapabilitiesAsync();       
        Task<bool> HealthCheckAsync();       
        Task InitializeAsync();      
        Task DisposeAsync();
    }
}
