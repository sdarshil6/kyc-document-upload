using KYCDocumentAPI.Core.Enums;

namespace KYCDocumentAPI.Infrastructure.Models
{
    public class DocumentClassificationResult
    {
        public DocumentType PredictedType { get; set; }
        public double Confidence { get; set; }
        public Dictionary<DocumentType, double> AllPredictions { get; set; } = new();
        public bool IsConfident => Confidence > 0.8;
        public string ProcessingNotes { get; set; } = string.Empty;
    }
}
