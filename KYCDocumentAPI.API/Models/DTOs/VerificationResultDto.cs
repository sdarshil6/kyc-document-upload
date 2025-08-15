using KYCDocumentAPI.Core.Enums;

namespace KYCDocumentAPI.API.Models.DTOs
{
    public class VerificationResultDto
    {
        public Guid Id { get; set; }
        public VerificationStatus Status { get; set; }
        public double AuthenticityScore { get; set; }
        public double QualityScore { get; set; }
        public double ConsistencyScore { get; set; }
        public double FraudScore { get; set; }
        public bool IsFormatValid { get; set; }
        public bool IsDataConsistent { get; set; }
        public bool IsImageClear { get; set; }
        public bool IsTampered { get; set; }
        public string? FailureReasons { get; set; }
        public string? AIInsights { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

}
