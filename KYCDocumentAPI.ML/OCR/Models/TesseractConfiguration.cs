using KYCDocumentAPI.ML.OCR.Enums;

namespace KYCDocumentAPI.ML.OCR.Models
{
    public class TesseractConfiguration
    {
        public string ExecutablePath { get; set; } = "tesseract";
        public string DataPath { get; set; } = string.Empty;
        public PageSegmentationMode PageSegMode { get; set; } = PageSegmentationMode.Auto;
        public OCREngineMode EngineMode { get; set; } = OCREngineMode.Default;
        public Dictionary<string, string> Variables { get; set; } = new();
        public bool PreserveInterwordSpaces { get; set; } = true;
        public int DPI { get; set; } = 300;
    }
}
