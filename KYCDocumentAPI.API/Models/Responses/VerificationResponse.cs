namespace KYCDocumentAPI.API.Models.Responses
{
    public class VerificationResponse
    {
        public Guid DocumentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public Dictionary<string, double> Scores { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
