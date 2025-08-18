using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IOCREngineFactory
    {        
        IOCREngine CreateEngine(OCREngine engineType);        
        List<IOCREngine> GetAvailableEngines();        
        Task<List<OCREngine>> GetHealthyEnginesAsync();       
        Task<OCREngine> GetOptimalEngineAsync(string documentType, ImageQualityMetrics quality);
    }
}
