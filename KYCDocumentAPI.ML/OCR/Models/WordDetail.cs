namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Word-level OCR details
    /// </summary>
    public class WordDetail
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
        public bool IsNumeric { get; set; }
        public bool IsAlphabetic { get; set; }
        public string Language { get; set; } = string.Empty;
    }
}
