using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.Data
{    
    public class DatasetBuilder
    {
        private readonly ILogger<DatasetBuilder> _logger;
        private readonly string _basePath;

        public DatasetBuilder(ILogger<DatasetBuilder> logger, string? basePath = null)
        {
            _logger = logger;
            _basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "TrainingData", "Images");
        }
        
        public async Task<bool> CreateFolderStructureAsync()
        {
            try
            {
                var documentTypes = new[]
                {
                    "Aadhaar", "PAN", "Passport", "DrivingLicense", "VoterID"
                };

                _logger.LogInformation("Creating training data folder structure at: {BasePath}", _basePath);

                foreach (var docType in documentTypes)
                {
                    var folderPath = Path.Combine(_basePath, docType);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                        _logger.LogInformation("Created folder: {FolderPath}", folderPath);
                        
                        var readmePath = Path.Combine(folderPath, "README.txt");
                        await File.WriteAllTextAsync(readmePath,
                            $"Place {docType} document images in this folder.\n" +
                            $"Supported formats: .jpg, .jpeg, .png\n" +
                            $"Recommended: At least 50-100 images per document type\n" +
                            $"Image requirements:\n" +
                            $"- Clear, readable text\n" +
                            $"- Good lighting and contrast\n" +
                            $"- Minimal blur or distortion\n" +
                            $"- Various angles and qualities for robustness\n\n" +
                            $"Example filenames:\n" +
                            $"- {docType.ToLower()}_sample_001.jpg\n" +
                            $"- {docType.ToLower()}_sample_002.png\n" +
                            $"- {docType.ToLower()}_document_003.jpg");
                    }
                }
                
                var mainReadmePath = Path.Combine(Path.GetDirectoryName(_basePath)!, "TRAINING_DATA_GUIDE.md");
                await File.WriteAllTextAsync(mainReadmePath, GetTrainingDataGuide());

                _logger.LogInformation("Folder structure created successfully!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder structure");
                return false;
            }
        }
        
        public async Task<(bool isValid, List<string> issues)> ValidateStructureAsync()
        {
            var issues = new List<string>();

            try
            {
                if (!Directory.Exists(_basePath))
                {
                    issues.Add($"Training data base path does not exist: {_basePath}");
                    return (false, issues);
                }

                var requiredFolders = new[]
                {
                    "Aadhaar", "PAN", "Passport", "DrivingLicense","VoterID"
                };

                foreach (var folder in requiredFolders)
                {
                    var folderPath = Path.Combine(_basePath, folder);
                    if (!Directory.Exists(folderPath))
                    {
                        issues.Add($"Missing required folder: {folder}");
                    }
                    else
                    {                       
                        var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(file => IsImageFile(file)).ToList();

                        if (imageFiles.Count == 0)
                        {
                            issues.Add($"Folder '{folder}' contains no image files");
                        }
                        else if (imageFiles.Count < 10)
                        {
                            issues.Add($"Folder '{folder}' has only {imageFiles.Count} images (recommend at least 50)");
                        }
                    }
                }

                await Task.CompletedTask;
                return (issues.Count == 0, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating structure: {ex.Message}");
                return (false, issues);
            }
        }
        
        public async Task<Dictionary<string, object>> GetDatasetStatsAsync()
        {
            var stats = new Dictionary<string, object>();

            try
            {
                if (!Directory.Exists(_basePath))
                {
                    stats["Error"] = "Training data path does not exist";
                    return stats;
                }

                var documentTypes = new[]
                {
                    "Aadhaar", "PAN", "Passport", "DrivingLicense",
                    "VoterID", "RationCard", "BankPassbook", "UtilityBill", "Other"
                };

                var totalImages = 0;
                var classDistribution = new Dictionary<string, int>();

                foreach (var docType in documentTypes)
                {
                    var folderPath = Path.Combine(_basePath, docType);
                    if (Directory.Exists(folderPath))
                    {
                        var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(file => IsImageFile(file)).ToList();

                        classDistribution[docType] = imageFiles.Count;
                        totalImages += imageFiles.Count;
                    }
                    else
                    {
                        classDistribution[docType] = 0;
                    }
                }

                stats["TotalImages"] = totalImages;
                stats["ClassDistribution"] = classDistribution;
                stats["NumberOfClasses"] = documentTypes.Length;
                stats["IsBalanced"] = IsDatasetBalanced(classDistribution);
                stats["HasSufficientData"] = totalImages >= 200;
                stats["RecommendedMinimum"] = 500;
                stats["LastUpdated"] = DateTime.UtcNow;

                await Task.CompletedTask;
                return stats;
            }
            catch (Exception ex)
            {
                stats["Error"] = ex.Message;
                return stats;
            }
        }
        
        private string GetTrainingDataGuide()
        {
            return @"# ML.NET Training Data Guide for KYC Document Classification

            ## Overview
            This guide helps you prepare training data for the AI document classification system.

            ## Folder Structure
            ```
            TrainingData/Images/
            ├── Aadhaar/          # Aadhaar card images
            ├── PAN/              # PAN card images  
            ├── Passport/         # Passport images
            ├── DrivingLicense/   # Driving license images
            ├── VoterID/          # Voter ID card images
            ```

            ## Data Collection Guidelines

            ### Minimum Requirements
            - **At least 50 images per document type** (recommended: 100+)
            - **Total minimum: 450 images** across all categories
            - **For production: 1000+ images** for best accuracy

            ### Image Quality Standards
            - **Resolution**: Minimum 300x300 pixels, recommended 800x600+
            - **Format**: JPG, PNG (JPEG preferred for smaller file sizes)
            - **Clarity**: Clear, readable text and logos
            - **Lighting**: Good contrast, avoid shadows and glare
            - **Orientation**: Upright documents (some rotation variance is OK)

            ### Diversity Requirements
            - **Multiple angles**: Slight rotations (±15 degrees)
            - **Various qualities**: Mix of high and medium quality images
            - **Different backgrounds**: White, wooden tables, scanner backgrounds
            - **Lighting conditions**: Good lighting, some variance acceptable
            - **Document variations**: Different states, formats, older vs newer versions

            ### Document-Specific Tips

            #### Aadhaar Cards
            - Include both old and new Aadhaar formats
            - Ensure UID numbers are visible (can be blurred for privacy)
            - Include cards with and without photos
            - Mix of languages (English + regional)

            #### PAN Cards
            - Include various PAN card designs over the years
            - Ensure PAN number format is clear
            - Include both individual and company PANs
            - Mix of old and new formats

            #### Passports  
            - Include multiple passport pages (photo page, visa pages)
            - Various passport colors (blue, maroon for different types)
            - Include both new and old passport formats
            - Ensure passport numbers are visible

            ### Data Organization Tips
            1. **Consistent naming**: Use descriptive filenames
               - Good: `aadhaar_sample_001.jpg`
               - Bad: `IMG_1234.jpg`

            2. **Quality check**: Review each image for clarity
            3. **Balance dataset**: Similar number of images per category
            4. **Remove duplicates**: Avoid nearly identical images
            5. **Privacy**: Blur or remove personal information if needed

            ### Testing Your Dataset
            After collecting data, run the validation:
            ```bash
            # This will check your data structure and quality
            dotnet run --project KYCDocumentAPI.ML -- validate-data
            ```

            ### Common Issues and Solutions
            - **Low accuracy**: Add more diverse training images
            - **Poor quality detection**: Include more varied image qualities
            - **Misclassification**: Check for mislabeled images
            - **Overfitting**: Add more validation data

            ### Free Image Sources (Use Responsibly)
            - Government websites with sample documents
            - Create mock documents (for testing only)
            - Use AI-generated sample documents (clearly labeled)
            - Academic datasets (with proper attribution)

            **Important**: Always respect privacy and legal requirements when collecting real document images.

            ## Next Steps
            1. Collect and organize your training images
            2. Run data validation to check quality
            3. Start model training with: `dotnet run --project KYCDocumentAPI.ML -- train`
            4. Evaluate results and iterate as needed

            For technical support, see the ML.NET documentation or project README.
            ";
        }

        private bool IsImageFile(string filePath)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return validExtensions.Contains(extension);
        }

        private bool IsDatasetBalanced(Dictionary<string, int> distribution)
        {
            if (distribution.Values.All(x => x == 0)) return false;

            var nonZeroValues = distribution.Values.Where(x => x > 0).ToList();
            if (nonZeroValues.Count < 2) return false;

            var min = nonZeroValues.Min();
            var max = nonZeroValues.Max();
           
            return (double)max / min <= 3.0;
        }
    }
}