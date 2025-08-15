using Microsoft.ML.Data;

namespace KYCDocumentAPI.ML.Models
{
    public class DocumentImageData
    {
        [LoadColumn(0)]
        public string ImagePath { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Label { get; set; } = string.Empty;
    }

    public class DocumentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDocumentType { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();

        public float Confidence => Score?.Max() ?? 0f;
    }

    // Feature extraction model for images
    public class ImageFeatures
    {
        [VectorType(1000)] // Assuming 1000 features from image analysis
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    public class DocumentTextFeatures
    {
        public string ExtractedText { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
    }

    public class DocumentClassificationInput
    {
        public string ImagePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;

        // Derived features
        public bool HasAadhaarPattern { get; set; }
        public bool HasPANPattern { get; set; }
        public bool HasPassportPattern { get; set; }
        public bool HasLicensePattern { get; set; }
        public float TextConfidence { get; set; }
        public float ImageQuality { get; set; }
    }

    public class DocumentClassificationOutput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDocumentType { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] ClassProbabilities { get; set; } = Array.Empty<float>();

        public float Confidence => ClassProbabilities?.Max() ?? 0f;

        public Dictionary<string, float> GetAllProbabilities()
        {
            var documentTypes = new[] { "Aadhaar", "PAN", "Passport", "DrivingLicense", "VoterID", "Other" };
            var probabilities = new Dictionary<string, float>();

            if (ClassProbabilities != null)
            {
                for (int i = 0; i < Math.Min(documentTypes.Length, ClassProbabilities.Length); i++)
                {
                    probabilities[documentTypes[i]] = ClassProbabilities[i];
                }
            }

            return probabilities;
        }
    }
}
