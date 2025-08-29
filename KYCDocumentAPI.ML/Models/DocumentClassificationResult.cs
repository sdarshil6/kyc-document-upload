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
        public string ProcessingNotes { get; set; } = string.Empty;                        
        public bool RequiresManualReview { get; set; }
        public List<string> ConfidenceFactors { get; set; } = new();

        public string GetSummary()
        {
            return $"Predicted: {PredictedDocumentType} ({Confidence:P1} confidence) | " +                  
                   $"Processing: {ProcessingTime.TotalMilliseconds:F0}ms | " +
                   $"Status: {(IsConfident ? "CONFIDENT" : "UNCERTAIN")}";
        }
    }
}
