namespace KYCDocumentAPI.ML.Models
{
    public class ModelEvaluationResult
    {
        public float OverallAccuracy { get; set; }
        public float MacroAverageAccuracy { get; set; }
        public float MicroAverageAccuracy { get; set; }
        public Dictionary<string, ClassMetrics> ClassMetrics { get; set; } = new();
        public string[,] ConfusionMatrix { get; set; } = new string[0, 0];
        public List<string> TopErrors { get; set; } = new();
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;
        public TimeSpan TrainingTime { get; set; }
        public bool MeetsQualityThreshold { get; set; }

        public string GetSummary()
        {
            return $"Overall Accuracy: {OverallAccuracy:P2} | " +
                   $"Training Time: {TrainingTime:mm\\:ss} | " +
                   $"Quality Check: {(MeetsQualityThreshold ? "PASSED" : "FAILED")}";
        }
    }
}
