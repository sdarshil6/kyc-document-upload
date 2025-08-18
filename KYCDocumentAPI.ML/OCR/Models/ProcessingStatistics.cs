namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Processing performance statistics
    /// </summary>
    public class ProcessingStatistics
    {
        public TimeSpan ImagePreprocessingTime { get; set; }
        public TimeSpan PrimaryEngineTime { get; set; }
        public TimeSpan FallbackEngineTime { get; set; }
        public TimeSpan PostProcessingTime { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public int MemoryUsageMB { get; set; }
        public bool UsedFallback { get; set; }
        public string ProcessingMode { get; set; } = string.Empty;
    }
}
