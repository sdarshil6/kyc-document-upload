namespace KYCDocumentAPI.ML.Models
{
    public static class StatisticsExtensions
    {
        public static float Variance(this float[] values)
        {
            if (values.Length <= 1) return 0;

            var mean = values.Average();
            var sumSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));
            return (float)(sumSquaredDifferences / (values.Length - 1));
        }
    }
}
