using KYCDocumentAPI.ML.Models;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.Services
{
    public class DocumentClassificationService : IDocumentClassificationService
    {        
        private readonly ILogger<DocumentClassificationService> _logger;                                
        private readonly IMLModelTrainingService _mlModelTrainingService;

        public DocumentClassificationService(ILogger<DocumentClassificationService> logger, IMLModelTrainingService mlModelTrainingService)
        {         
            _logger = logger;           
            _mlModelTrainingService = mlModelTrainingService;
        }                          

        public async Task<ImagePrediction> ClassifyDocumentAsync(string filePath)
        {
            try
            {                                                                            
                return await _mlModelTrainingService.PredictAsync(filePath);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying document {FilePath}", filePath);
                throw;
            }
        }
    }    
}
