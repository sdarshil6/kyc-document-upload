using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Comprehensive OCR result with multiple engine support
    /// </summary>
    public class EnhancedOCRResult
    {
        public bool Success { get; set; }
        public string ExtractedText { get; set; } = string.Empty;
        public float OverallConfidence { get; set; }
        public OCREngine PrimaryEngine { get; set; }
        public OCREngine? FallbackEngine { get; set; }
        public List<string> DetectedLanguages { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        // Engine-specific results
        public List<EngineResult> EngineResults { get; set; } = new();

        // Quality metrics
        public ImageQualityMetrics? QualityMetrics { get; set; }

        // Text analysis
        public TextAnalysisResult? TextAnalysis { get; set; }

        // Processing statistics
        public ProcessingStatistics ProcessingStats { get; set; } = new();
    }
}
