namespace KYCDocumentAPI.ML.Models
{
    public class EnhancedImageData : ImageData
    {        
        public float ImageWidth { get; set; }
        public float ImageHeight { get; set; }
        public float AspectRatio { get; set; }
        public float Brightness { get; set; }
        public float Contrast { get; set; }
        public bool IsColor { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        
        public bool HasText { get; set; }
        public float TextDensity { get; set; }
        public bool HasLogo { get; set; }
        public bool HasPhoto { get; set; } 
        
        public float BlurScore { get; set; }
        public float NoiseLevel { get; set; }
        public bool IsHighQuality { get; set; }

        public static EnhancedImageData FromImageData(ImageData imageData)
        {
            return new EnhancedImageData
            {
                ImagePath = imageData.ImagePath,
                Label = imageData.Label,
                FileSize = imageData.FileSize,
                OriginalFileName = imageData.OriginalFileName,
                CreatedDate = imageData.CreatedDate              
            };
        }
    }
}
