using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// OCR engine capabilities
    /// </summary>
    public class OCREngineCapabilities
    {
        public OCREngine Engine { get; set; }
        public string Version { get; set; } = string.Empty;
        public List<string> SupportedLanguages { get; set; } = new();
        public List<string> SupportedFormats { get; set; } = new();
        public bool SupportsWordDetails { get; set; }
        public bool SupportsConfidenceScores { get; set; }
        public bool SupportsMultipleLanguages { get; set; }
        public bool SupportsHandwriting { get; set; }
        public float AverageAccuracy { get; set; }
        public float AverageSpeed { get; set; } // Processing speed in pages per second
        public Dictionary<string, object> AdditionalCapabilities { get; set; } = new();
    }
}
