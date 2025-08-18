using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    public class DocumentTypeSettings
    {
        public Dictionary<string, OCRProcessingOptions> DocumentConfigurations { get; set; } = new()
        {
            ["Aadhaar"] = new OCRProcessingOptions
            {
                Languages = new List<string> { "en", "hi" },
                PreferredEngine = OCREngine.EasyOCR,
                EnableFallback = true,
                PreprocessImage = true,
                MinimumConfidence = 0.6f
            },
            ["PAN"] = new OCRProcessingOptions
            {
                Languages = new List<string> { "en" },
                PreferredEngine = OCREngine.Tesseract,
                EnableFallback = true,
                PreprocessImage = true,
                MinimumConfidence = 0.7f
            },
            ["Passport"] = new OCRProcessingOptions
            {
                Languages = new List<string> { "en" },
                PreferredEngine = OCREngine.EasyOCR,
                EnableFallback = true,
                PreprocessImage = true,
                MinimumConfidence = 0.8f
            }
        };
    }
}
