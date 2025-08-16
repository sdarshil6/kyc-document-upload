namespace KYCDocumentAPI.Infrastructure.Models
{
    public static class CacheKeys
    {
        public const string USER_PREFIX = "user:";
        public const string DOCUMENT_PREFIX = "document:";
        public const string ANALYTICS_PREFIX = "analytics:";
        public const string FRAUD_PREFIX = "fraud:";
        public const string AI_STATUS = "ai:status";

        public static string UserById(Guid userId) => $"{USER_PREFIX}{userId}";
        public static string UserDocuments(Guid userId) => $"{USER_PREFIX}{userId}:documents";
        public static string DocumentById(Guid documentId) => $"{DOCUMENT_PREFIX}{documentId}";
        public static string DashboardAnalytics() => $"{ANALYTICS_PREFIX}dashboard";
        public static string FraudAnalytics(DateTime date) => $"{FRAUD_PREFIX}analytics:{date:yyyy-MM-dd}";
        public static string AIServiceStatus() => AI_STATUS;
    }
}
