namespace KYCDocumentAPI.API.Models.Requests
{
    public class BatchValidationRequest
    {
        [Required]
        public List<Guid> DocumentIds { get; set; } = new();

        public bool IncludeDetailedResults { get; set; } = false;

        public string? Priority { get; set; } = "Normal"; // Normal, High, Critical
    }
}
