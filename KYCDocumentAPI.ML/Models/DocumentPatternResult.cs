using KYCDocumentAPI.Core.Enums;

namespace KYCDocumentAPI.ML.Models
{
    public class DocumentPatternResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DocumentType PredictedDocumentType { get; set; }
        public float Confidence { get; set; }
        public Dictionary<DocumentType, float> DocumentTypeConfidences { get; set; } = new();

        public string AadhaarNumber { get; set; } = string.Empty;
        public string PANNumber { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;

        public bool HasAadhaarPattern { get; set; }
        public bool HasPANPattern { get; set; }
        public bool HasPassportPattern { get; set; }
    }
}
