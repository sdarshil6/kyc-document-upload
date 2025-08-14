using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KYCDocumentAPI.Core.Entities
{
    public class KYCVerification : BaseEntity
    {
        public Guid UserId { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public int RejectedDocuments { get; set; }

        public double OverallScore { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime? CompletedAt { get; set; }

        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<VerificationResult> VerificationResults { get; set; } = new List<VerificationResult>();
    }
}
