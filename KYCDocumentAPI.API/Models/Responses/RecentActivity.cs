namespace KYCDocumentAPI.API.Models.Responses
{
    public class RecentActivity
    {
        public string Activity { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
