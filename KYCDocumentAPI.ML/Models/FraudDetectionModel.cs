using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.ML.Enums;
using Microsoft.ML.Data;

namespace KYCDocumentAPI.ML.Models
{
    public class FraudDetectionInput
    {        
        public string DocumentType { get; set; } = string.Empty;
        public float FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        
        public float OCRConfidence { get; set; }
        public float TextQuality { get; set; }
        public int TextLength { get; set; }
        public float LanguageConsistency { get; set; }
        
        public float ImageBrightness { get; set; }
        public float ImageContrast { get; set; }
        public float ImageSharpness { get; set; }
        public float NoiseLevel { get; set; }
        
        public bool HasValidNumberFormat { get; set; }
        public bool HasConsistentDateFormats { get; set; }
        public bool HasExpectedDocumentStructure { get; set; }
       
        public float NameConsistency { get; set; }
        public float DateConsistency { get; set; }
        public float AddressConsistency { get; set; }
       
        public float NumberDistribution { get; set; }
        public float TextDistribution { get; set; }
        public float ColorDistribution { get; set; }
        
        public bool HasUnexpectedFonts { get; set; }
        public bool HasInconsistentSpacing { get; set; }
        public bool HasColorAnomalies { get; set; }
        public bool HasCompressionArtifacts { get; set; }
        
        public float UserHistoryScore { get; set; }
        public float DocumentHistoryScore { get; set; }
    }

    public class FraudDetectionOutput
    {
        [ColumnName("PredictedLabel")]
        public bool IsFraudulent { get; set; }

        [ColumnName("Probability")]
        public float FraudProbability { get; set; }

        [ColumnName("Score")]
        public float FraudScore { get; set; }
    }

    public class VerificationMetrics
    {
        public float AuthenticityScore { get; set; }
        public float QualityScore { get; set; }
        public float ConsistencyScore { get; set; }
        public float FraudRiskScore { get; set; }
        public float OverallScore { get; set; }

        public Dictionary<string, float> DetailedScores { get; set; } = new();
        public List<string> RiskFactors { get; set; } = new();
        public List<string> QualityIssues { get; set; } = new();
        public List<string> PositiveIndicators { get; set; } = new();
    }

    public class DocumentValidationResult
    {
        public bool IsValid { get; set; }
        public VerificationStatus Status { get; set; }
        public VerificationMetrics Metrics { get; set; } = new();
        public List<ValidationCheck> Checks { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ProcessingTime { get; set; }
    }

    public class ValidationCheck
    {
        public string CheckName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public float Score { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public CheckSeverity Severity { get; set; }
    }        
}
