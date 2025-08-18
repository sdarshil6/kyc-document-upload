using KYCDocumentAPI.ML.OCR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public interface IOCRResultAnalyzer
    {        
        Task<EngineResult> SelectBestResultAsync(List<EngineResult> results);        
        Task<EngineResult> MergeResultsAsync(List<EngineResult> results);        
        Task<TextAnalysisResult> AnalyzeTextQualityAsync(string text, List<WordDetail> wordDetails);       
        Task<bool> ValidateResultAsync(EngineResult result, List<string> expectedPatterns);       
        float CalculateOverallConfidence(List<EngineResult> results);
    }
}
