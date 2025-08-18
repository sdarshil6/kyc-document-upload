namespace KYCDocumentAPI.ML.OCR.Models
{
    public class QualityThresholds
    {
        public float MinimumConfidence { get; set; } = 0.5f;
        public float HighQualityThreshold { get; set; } = 0.8f;
        public float MediumQualityThreshold { get; set; } = 0.6f;
        public float LowQualityThreshold { get; set; } = 0.4f;
        public float BrightnessMin { get; set; } = 0.3f;
        public float BrightnessMax { get; set; } = 0.8f;
        public float ContrastMin { get; set; } = 0.4f;
        public float SharpnessMin { get; set; } = 0.5f;
        public float NoiseMax { get; set; } = 0.3f;
    }
}
