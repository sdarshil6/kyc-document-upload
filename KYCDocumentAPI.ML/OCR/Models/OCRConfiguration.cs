using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.OCR.Models
{
    public class OCRConfiguration
    {
        public string TesseractPath { get; set; } = string.Empty;
        public string TesseractDataPath { get; set; } = string.Empty;                
        public List<string> DefaultLanguages { get; set; } = new() { "eng", "hin", "guj"};
        public int ProcessingTimeout { get; set; } = 30000; // 30 seconds
        public int MaxRetries { get; set; } = 3;
        public bool PreprocessImages { get; set; } = true;
        public bool EnableParallelProcessing { get; set; } = true;
        public bool CacheResults { get; set; } = true;
        public string TempDirectory { get; set; } = Path.GetTempPath();
        public int MaxConcurrentProcesses { get; set; } = Environment.ProcessorCount;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
