using Microsoft.ML.Data;

namespace KYCDocumentAPI.ML.Models
{
    public class ImageData
    {
        [LoadColumn(0)]
        public string ImagePath { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Label { get; set; } = string.Empty;
       
        public long FileSize { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsAugmented { get; set; } = false;
    }
}
