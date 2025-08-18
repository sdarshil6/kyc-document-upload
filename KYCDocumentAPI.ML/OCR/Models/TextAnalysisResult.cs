using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Text analysis and patterns
    /// </summary>
    public class TextAnalysisResult
    {
        public int TotalCharacters { get; set; }
        public int TotalWords { get; set; }
        public int TotalLines { get; set; }
        public float AverageWordConfidence { get; set; }
        public Dictionary<string, float> LanguageDistribution { get; set; } = new();
        public List<string> DetectedPatterns { get; set; } = new();
        public bool HasNumbers { get; set; }
        public bool HasDates { get; set; }
        public bool HasEmails { get; set; }
        public bool HasPhoneNumbers { get; set; }
        public TextComplexity Complexity { get; set; }
    }
}
