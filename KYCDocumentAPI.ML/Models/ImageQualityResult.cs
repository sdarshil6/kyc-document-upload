namespace KYCDocumentAPI.ML.Models
{
    public class ImageQualityResult
    {
        public float OverallQuality { get; set; }
        public float Brightness { get; set; }
        public float Contrast { get; set; }
        public float Sharpness { get; set; }
        public float NoiseLevel { get; set; }
        public bool IsBlurry { get; set; }
        public bool IsTooDark { get; set; }
        public bool IsTooLight { get; set; }
        public List<string> QualityIssues { get; set; } = new();
    }
}
