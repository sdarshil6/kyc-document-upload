namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Image preprocessing configuration
    /// </summary>
    public class ImagePreprocessingOptions
    {
        public bool AutoRotate { get; set; } = true;
        public bool EnhanceContrast { get; set; } = true;
        public bool ReduceNoise { get; set; } = true;
        public bool NormalizeSize { get; set; } = true;
        public bool CorrectSkew { get; set; } = true;
        public int TargetDPI { get; set; } = 300;
        public float ContrastFactor { get; set; } = 1.2f;
        public int MaxWidth { get; set; } = 2048;
        public int MaxHeight { get; set; } = 2048;
    }
}
