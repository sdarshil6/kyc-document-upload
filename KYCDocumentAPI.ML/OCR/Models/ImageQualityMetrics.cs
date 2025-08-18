namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Image quality assessment metrics
    /// </summary>
    public class ImageQualityMetrics
    {
        public float OverallQuality { get; set; }
        public float Brightness { get; set; }
        public float Contrast { get; set; }
        public float Sharpness { get; set; }
        public float NoiseLevel { get; set; }
        public float Resolution { get; set; }
        public bool IsBlurry { get; set; }
        public bool IsTooDark { get; set; }
        public bool IsTooLight { get; set; }
        public bool HasSufficientContrast { get; set; }
        public List<string> QualityIssues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
