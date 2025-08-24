namespace KYCDocumentAPI.ML.Models
{
    public class TrainingDataStats
    {
        public int TotalImages { get; set; }
        public Dictionary<string, int> ImagesPerClass { get; set; } = new();
        public Dictionary<string, List<string>> ImagePaths { get; set; } = new();
        public List<string> InvalidImages { get; set; } = new();
        public List<string> MissingClasses { get; set; } = new();        
        public bool HasSufficientData { get; set; }
        public List<string> Recommendations { get; set; } = new();

        public string GetSummary()
        {
            var classInfo = string.Join(", ", ImagesPerClass.Select(x => $"{x.Key}: {x.Value}"));
            return $"Total: {TotalImages} images | Classes: {classInfo} | " + $" Sufficient: {HasSufficientData}";
        }
    }
}
