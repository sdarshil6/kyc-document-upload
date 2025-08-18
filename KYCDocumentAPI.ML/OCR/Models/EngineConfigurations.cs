namespace KYCDocumentAPI.ML.OCR.Models
{
    public class EngineConfigurations
    {
        public TesseractConfiguration Tesseract { get; set; } = new();
        public EasyOCRConfiguration EasyOCR { get; set; } = new();
    }
}
