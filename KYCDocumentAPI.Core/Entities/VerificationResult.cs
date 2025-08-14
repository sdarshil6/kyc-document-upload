using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KYCDocumentAPI.Core.Entities
{
    public class VerificationResult : BaseEntity
    {
        public Guid DocumentId { get; set; }
        public Guid? KYCVerificationId { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        
        public double AuthenticityScore { get; set; }
        public double QualityScore { get; set; }
        public double ConsistencyScore { get; set; }
        public double FraudScore { get; set; }

       
        public bool IsFormatValid { get; set; }
        public bool IsDataConsistent { get; set; }
        public bool IsImageClear { get; set; }
        public bool IsTampered { get; set; }

        [MaxLength(2000)]
        public string? FailureReasons { get; set; }

        [MaxLength(1000)]
        public string? AIInsights { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

       
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;

        [ForeignKey("KYCVerificationId")]
        public virtual KYCVerification? KYCVerification { get; set; }
    }
}
