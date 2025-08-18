using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class TesseractEngine : IOCREngine
    {
        public OCREngine EngineType => throw new NotImplementedException();

        public Task DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<EngineResult> ExtractTextAsync(string imagePath, OCRProcessingOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<EngineResult> ExtractTextAsync(Stream imageStream, OCRProcessingOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<OCREngineCapabilities> GetCapabilitiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<OCREngineStatus> GetStatusAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HealthCheckAsync()
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
