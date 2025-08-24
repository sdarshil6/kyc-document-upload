using Microsoft.ML.Data;

namespace KYCDocumentAPI.ML.Models
{
    public class ImagePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Scores { get; set; } = Array.Empty<float>();
       
        public float Confidence => Scores?.Max() ?? 0f;

        public Dictionary<string, float> GetAllProbabilities()
        {
            var labels = new[]
            {
                "Aadhaar", "PAN", "Passport", "DrivingLicense",
                "VoterID", "RationCard", "BankPassbook", "UtilityBill", "Other"
            };

            var probabilities = new Dictionary<string, float>();

            if (Scores != null && Scores.Length > 0)
            {
                for (int i = 0; i < Math.Min(labels.Length, Scores.Length); i++)
                {
                    probabilities[labels[i]] = Scores[i];
                }
            }

            return probabilities;
        }

        public bool IsConfident(float threshold = 0.7f)
        {
            return Confidence >= threshold;
        }

        public string GetTopPredictions(int count = 3)
        {
            var probabilities = GetAllProbabilities()
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => $"{x.Key}: {x.Value:P1}")
                .ToList();

            return string.Join(", ", probabilities);
        }
    }
}
