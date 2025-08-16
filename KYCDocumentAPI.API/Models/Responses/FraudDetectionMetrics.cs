namespace KYCDocumentAPI.API.Models.Responses
{
    public class FraudDetectionMetrics
    {
        public double FraudRate { get; set; }
        public double SuspiciousRate { get; set; }
        public int TotalFlagged { get; set; }
    }
}
