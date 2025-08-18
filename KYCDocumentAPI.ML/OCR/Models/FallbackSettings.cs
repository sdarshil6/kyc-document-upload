using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    public class FallbackSettings
    {
        public bool EnableFallback { get; set; } = true;
        public OCREngine PrimaryEngine { get; set; } = OCREngine.EasyOCR;
        public OCREngine FallbackEngine { get; set; } = OCREngine.Tesseract;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public float MinConfidenceForFallback { get; set; } = 0.6f;
        public bool CompareResults { get; set; } = true;
        public float MinSimilarityThreshold { get; set; } = 0.8f;
    }
}
