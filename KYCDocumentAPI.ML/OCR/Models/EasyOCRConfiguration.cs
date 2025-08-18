namespace KYCDocumentAPI.ML.OCR.Models
{
    public class EasyOCRConfiguration
    {
        public string PythonExecutable { get; set; } = "python";
        public string ScriptPath { get; set; } = "Scripts/easyocr_processor.py";
        public bool UseGPU { get; set; } = false;
        public float ConfidenceThreshold { get; set; } = 0.5f;
        public int Width { get; set; } = 0; // 0 = auto
        public int Height { get; set; } = 0; // 0 = auto
        public bool AllowList { get; set; } = false;
        public string AllowedCharacters { get; set; } = string.Empty;
        public bool BlockList { get; set; } = false;
        public string BlockedCharacters { get; set; } = string.Empty;
    }
}
