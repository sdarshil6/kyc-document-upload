using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// OCR processing configuration
    /// </summary>
    public class OCRProcessingOptions
    {
        public List<string> Languages { get; set; } = new() { "en", "hi" };
        public OCREngine PreferredEngine { get; set; } = OCREngine.EasyOCR;
        public bool EnableFallback { get; set; } = true;
        public bool PreprocessImage { get; set; } = true;
        public bool AnalyzeQuality { get; set; } = true;
        public bool ExtractWordDetails { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public float MinimumConfidence { get; set; } = 0.5f;
        public ImagePreprocessingOptions PreprocessingOptions { get; set; } = new();
    }
}
