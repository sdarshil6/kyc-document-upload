namespace KYCDocumentAPI.ML.Models
{
    public class TrainingProgress
    {
        public int CurrentEpoch { get; set; }
        public int TotalEpochs { get; set; }
        public float CurrentAccuracy { get; set; }
        public float CurrentLoss { get; set; }
        public float ValidationAccuracy { get; set; }
        public float ValidationLoss { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedRemainingTime { get; set; }
        public string CurrentPhase { get; set; } = "Training";
        public Dictionary<string, float> ClassAccuracies { get; set; } = new();

        public double ProgressPercentage => TotalEpochs > 0 ? (double)CurrentEpoch / TotalEpochs * 100 : 0;

        public override string ToString()
        {
            return $"Epoch {CurrentEpoch}/{TotalEpochs} ({ProgressPercentage:F1}%) - " +
                   $"Accuracy: {CurrentAccuracy:F3}, Loss: {CurrentLoss:F3}, " +
                   $"Val Acc: {ValidationAccuracy:F3}, Elapsed: {ElapsedTime:mm\\:ss}";
        }
    }
}
