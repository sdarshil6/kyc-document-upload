namespace KYCDocumentAPI.API.Models.Responses
{
    public class DashboardResponse
    {
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public int PendingVerifications { get; set; }
        public int FraudulentDocuments { get; set; }
        public int SuspiciousDocuments { get; set; }
        public Dictionary<string, int> DocumentTypeDistribution { get; set; } = new();
        public Dictionary<string, int> StateDistribution { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
        public FraudDetectionMetrics FraudDetectionMetrics { get; set; } = new();
    }
}
