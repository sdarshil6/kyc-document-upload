using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Individual OCR engine result
    /// </summary>
    public class EngineResult
    {
        public OCREngine Engine { get; set; }
        public bool Success { get; set; }
        public string ExtractedText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> EngineSpecificData { get; set; } = new();
        
        public List<WordDetail> WordDetails { get; set; } = new();
    }
}
