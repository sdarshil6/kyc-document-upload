using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KYCDocumentAPI.Core.Entities
{
    public class DocumentData : BaseEntity
    {
        public Guid DocumentId { get; set; }

       
        [MaxLength(100)]
        public string? FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        
        [MaxLength(12)]
        public string? AadhaarNumber { get; set; }

        
        [MaxLength(10)]
        public string? PANNumber { get; set; }

       
        [MaxLength(20)]
        public string? PassportNumber { get; set; }

        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        
        [MaxLength(2000)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? PinCode { get; set; }

        
        public string? RawExtractedData { get; set; }

        
        public double ExtractionConfidence { get; set; }

        
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;
    }
}
