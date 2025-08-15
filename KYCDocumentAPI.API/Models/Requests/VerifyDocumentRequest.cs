using System.ComponentModel.DataAnnotations;

namespace KYCDocumentAPI.API.Models.Requests
{
    public class VerifyDocumentRequest
    {
        [Required]
        public Guid DocumentId { get; set; }

        public bool RunFraudDetection { get; set; } = true;

        public bool RunQualityCheck { get; set; } = true;

        public bool RunConsistencyCheck { get; set; } = true;
    }
}
