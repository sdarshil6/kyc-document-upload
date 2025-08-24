namespace KYCDocumentAPI.ML.Models
{
    public class MLConfig
    {
        // Training Configuration
        public int TrainingEpochs { get; set; } = 100;
        public int BatchSize { get; set; } = 32;
        public float LearningRate { get; set; } = 0.001f;
        public int ImageHeight { get; set; } = 224;
        public int ImageWidth { get; set; } = 224;
        public string Architecture { get; set; } = "ResnetV250"; 

        // Data Configuration
        public string TrainingDataPath { get; set; } = "TrainingData/Images";
        public string ModelOutputPath { get; set; } = "Models/Trained/DocumentClassifier.zip";
        public string TempModelPath { get; set; } = "Models/Temp";
        public float ValidationSplit { get; set; } = 0.2f;

        // Performance Configuration
        public int MaxConcurrentTraining { get; set; } = Environment.ProcessorCount;
        public bool UseGpu { get; set; } = false;
        public int CacheSize { get; set; } = 100;

        // Classification Configuration
        public float MinimumConfidenceThreshold { get; set; } = 0.7f;
        public int NumberOfClasses { get; set; } = 9;
        public string[] ClassLabels { get; set; } = new[]
        {
            "Aadhaar",
            "PAN",
            "Passport",
            "DrivingLicense",
            "VoterID",
            "RationCard",
            "BankPassbook",
            "UtilityBill",
            "Other"
        };

        // Quality Assurance
        public float MinimumAccuracyThreshold { get; set; } = 0.85f; 
        public bool EnableCrossValidation { get; set; } = true;
        public int CrossValidationFolds { get; set; } = 5;

        // Production Settings
        public bool EnableModelMetrics { get; set; } = true;
        public bool EnableDetailedLogging { get; set; } = true;
        public int ModelRefreshIntervalHours { get; set; } = 24;

        // Data Augmentation Settings
        public bool EnableDataAugmentation { get; set; } = true;
        public float RotationRange { get; set; } = 15.0f; 
        public float BrightnessRange { get; set; } = 0.2f;
        public float ContrastRange { get; set; } = 0.2f;
        
        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            if (TrainingEpochs <= 0)
                errors.Add("TrainingEpochs must be greater than 0");

            if (BatchSize <= 0)
                errors.Add("BatchSize must be greater than 0");

            if (LearningRate <= 0 || LearningRate > 1)
                errors.Add("LearningRate must be between 0 and 1");

            if (ImageHeight <= 0 || ImageWidth <= 0)
                errors.Add("Image dimensions must be greater than 0");

            if (ValidationSplit <= 0 || ValidationSplit >= 1)
                errors.Add("ValidationSplit must be between 0 and 1");

            if (MinimumConfidenceThreshold < 0 || MinimumConfidenceThreshold > 1)
                errors.Add("MinimumConfidenceThreshold must be between 0 and 1");

            if (ClassLabels == null || ClassLabels.Length != NumberOfClasses)
                errors.Add($"ClassLabels must contain exactly {NumberOfClasses} labels");

            if (!Directory.Exists(TrainingDataPath))
                errors.Add($"Training data path does not exist: {TrainingDataPath}");

            return errors;
        }
       
        public static MLConfig GetProductionConfig()
        {
            return new MLConfig
            {
                TrainingEpochs = 150,
                BatchSize = 16, 
                LearningRate = 0.0005f,
                Architecture = "ResnetV250",
                MinimumAccuracyThreshold = 0.90f,
                EnableCrossValidation = true,
                EnableDataAugmentation = true,
                EnableDetailedLogging = true
            };
        }
        
        public static MLConfig GetDevelopmentConfig()
        {
            return new MLConfig
            {
                TrainingEpochs = 50,
                BatchSize = 32,
                LearningRate = 0.001f,
                Architecture = "MobilenetV2",
                MinimumAccuracyThreshold = 0.80f, 
                EnableCrossValidation = false,
                EnableDataAugmentation = false,
                EnableDetailedLogging = false
            };
        }
    }
}
