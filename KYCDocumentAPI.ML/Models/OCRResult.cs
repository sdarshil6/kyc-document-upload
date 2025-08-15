namespace KYCDocumentAPI.ML.Models
{
    public class OCRResult
    {
        public bool Success { get; set; }
        public string ExtractedText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public List<string> DetectedLanguages { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
    }
}
