namespace KYCDocumentAPI.ML.Models
{
    public class ClassMetrics
    {
        public string ClassName { get; set; } = string.Empty;
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float F1Score { get; set; }
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public int FalseNegatives { get; set; }
        public int Support { get; set; } // Number of actual instances

        public override string ToString()
        {
            return $"{ClassName}: P={Precision:F3}, R={Recall:F3}, F1={F1Score:F3}";
        }
    }
}
