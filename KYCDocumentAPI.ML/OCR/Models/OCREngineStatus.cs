using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// OCR engine status and health
    /// </summary>
    public class OCREngineStatus
    {
        public OCREngine Engine { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsHealthy { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime LastHealthCheck { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public float SuccessRate => (SuccessfulRequests + FailedRequests) > 0 ?
            (float)SuccessfulRequests / (SuccessfulRequests + FailedRequests) : 0f;
    }
}
