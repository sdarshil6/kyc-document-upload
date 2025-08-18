namespace KYCDocumentAPI.ML.OCR.Models
{
    public class PerformanceSettings
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);
        public int MaxCacheSize { get; set; } = 1000; // Number of cached results
        public bool EnableParallelProcessing { get; set; } = true;
        public int MaxParallelTasks { get; set; } = Environment.ProcessorCount;
        public bool PreloadModels { get; set; } = true;
        public bool OptimizeMemoryUsage { get; set; } = true;
        public int MemoryLimitMB { get; set; } = 512;
    }
}
