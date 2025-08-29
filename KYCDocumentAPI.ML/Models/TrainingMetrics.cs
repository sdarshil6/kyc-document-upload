namespace KYCDocumentAPI.ML.Models
{
    public class TrainingMetrics
    {
        public string ModelName { get; set; } = string.Empty;
        public DateTime TrainingStartTime { get; set; }
        public DateTime TrainingEndTime { get; set; }
        public TimeSpan TrainingDuration => TrainingEndTime - TrainingStartTime;
        public int TotalTrainingImages { get; set; }        
        public int NumberOfClasses { get; set; }
        public Dictionary<string, int> ImagesPerClass { get; set; } = new();       
        public float FinalAccuracy { get; set; }       
        public float ValidationAccuracy { get; set; }            
        public MLConfig Configuration { get; set; } = new();        
        public string ModelFilePath { get; set; } = string.Empty;
        public long ModelFileSize { get; set; }      
        public bool IsProductionReady { get; set; }
        public List<string> QualityIssues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();

        public string GetTrainingSummary()
        {
            return $"Model: {ModelName} | " +
                   $"Duration: {TrainingDuration:mm\\:ss} | " +
                   $"Images: {TotalTrainingImages} training, " +
                   $"Accuracy: {FinalAccuracy:P2} (validation: {ValidationAccuracy:P2}) | " +
                   $"Status: {(IsProductionReady ? "READY" : "NEEDS WORK")}";
        }

        public void ValidateQuality()
        {
            QualityIssues.Clear();
            Recommendations.Clear();
         
            if (FinalAccuracy < Configuration.MinimumAccuracyThreshold)
            {
                QualityIssues.Add($"Accuracy {FinalAccuracy:P2} below minimum {Configuration.MinimumAccuracyThreshold:P2}");
                Recommendations.Add("Increase training epochs or collect more training data");
            }
            
            var accuracyGap = Math.Abs(FinalAccuracy - ValidationAccuracy);
            if (accuracyGap > 0.1f)
            {
                QualityIssues.Add($"Large accuracy gap: {accuracyGap:P1} between training and validation");
                Recommendations.Add("Add regularization or more validation data to reduce overfitting");
            }
            
            var classVariance = ImagesPerClass.Values.Count > 0 ?
                ImagesPerClass.Values.Select(x => (float)x).ToArray().Variance() : 0;
            if (classVariance > 100)
            {
                QualityIssues.Add("Imbalanced training data detected");
                Recommendations.Add("Collect more examples for underrepresented classes");
            }
            
            if (TotalTrainingImages < NumberOfClasses * 20)
            {
                QualityIssues.Add("Insufficient training data");
                Recommendations.Add($"Collect at least {NumberOfClasses * 20} total training images");
            }

            IsProductionReady = QualityIssues.Count == 0 && FinalAccuracy >= Configuration.MinimumAccuracyThreshold;
        }
    }
}
