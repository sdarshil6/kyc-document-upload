using KYCDocumentAPI.ML.OCR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IEnhancedOCRService
    {        
        Task<EnhancedOCRResult> ExtractTextAsync(string imagePath, OCRProcessingOptions? options = null);        
        Task<EnhancedOCRResult> ExtractTextAsync(Stream imageStream, string fileName, OCRProcessingOptions? options = null);       
        Task<ImageQualityMetrics> AnalyzeImageQualityAsync(string imagePath);        
        Task<ImageQualityMetrics> AnalyzeImageQualityAsync(Stream imageStream);        
        Task<List<OCREngineStatus>> GetEngineStatusAsync();      
        Task<List<OCREngineCapabilities>> GetEngineCapabilitiesAsync();       
        Task<List<EnhancedOCRResult>> ProcessBatchAsync(List<string> imagePaths, OCRProcessingOptions? options = null);      
        Task<Dictionary<string, object>> GetPerformanceMetricsAsync();
    }
}
