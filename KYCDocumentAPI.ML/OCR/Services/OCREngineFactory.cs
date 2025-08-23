using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class OCREngineFactory : IOCREngineFactory
    {
        private readonly ILogger<OCREngineFactory> _logger;
        private readonly OCRConfiguration _configuration;
        private readonly TesseractOCREngine _tesseractEngine;

        public OCREngineFactory(ILogger<OCREngineFactory> logger, OCRConfiguration configuration, TesseractOCREngine tesseractEngine)
        {
            _logger = logger;
            _configuration = configuration;
            _tesseractEngine = tesseractEngine;
        }

        public IOCREngine CreateEngine()
        {
            try
            {
                return _tesseractEngine;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside CreateEngine() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }        

        public async Task<List<OCREngine>> GetHealthyEnginesAsync()
        {
            try
            {
                var healthyEngines = new List<OCREngine>();
                var engine = CreateEngine();
                if (await engine.HealthCheckAsync())
                    healthyEngines.Add(OCREngine.Tesseract);

                return healthyEngines;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetHealthyEnginesAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }        
    }
}
