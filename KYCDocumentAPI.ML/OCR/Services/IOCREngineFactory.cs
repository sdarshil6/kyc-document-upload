using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IOCREngineFactory
    {        
        IOCREngine CreateEngine();
        Task<List<OCREngine>> GetHealthyEnginesAsync();        
    }
}
