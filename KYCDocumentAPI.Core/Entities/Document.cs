using KYCDocumentAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KYCDocumentAPI.Core.Entities
{
    public class Document : BaseEntity
    {
        [Required]
        public DocumentType DocumentType { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
        
        public Guid UserId { get; set; }

        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual DocumentData? DocumentData { get; set; }
        public virtual ICollection<VerificationResult> VerificationResults { get; set; } = new List<VerificationResult>();
    }
}
