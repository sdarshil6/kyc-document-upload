namespace KYCDocumentAPI.ML.Models
{
    public class DocumentClassificationResult
    {
        public string PredictedDocumentType { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Dictionary<string, float> AllProbabilities { get; set; } = new();
        public bool IsConfident { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
       
        public string ModelVersion { get; set; } = string.Empty;
        public string ProcessingNotes { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public float ImageQualityScore { get; set; }
        public bool RequiresManualReview { get; set; }
        public List<string> ConfidenceFactors { get; set; } = new();

        public string GetSummary()
        {
            return $"Predicted: {PredictedDocumentType} ({Confidence:P1} confidence) | " +
                   $"Quality: {ImageQualityScore:P1} | " +
                   $"Processing: {ProcessingTime.TotalMilliseconds:F0}ms | " +
                   $"Status: {(IsConfident ? "CONFIDENT" : "UNCERTAIN")}";
        }
    }
}
