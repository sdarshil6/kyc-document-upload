using KYCDocumentAPI.Core.Enums;

using KYCDocumentAPI.ML.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace KYCDocumentAPI.ML.Services
{
    public class DocumentClassificationService : IDocumentClassificationService, IDisposable
    {
        private readonly MLContext _mlContext;
        private readonly ILogger<DocumentClassificationService> _logger;                
        private ITransformer? _model;
        private object? _predictionEngine; // placeholder for now
        private readonly IServiceProvider _provider;

        public bool IsModelReady => _model != null && _predictionEngine != null;

        public DocumentClassificationService(ILogger<DocumentClassificationService> logger, IServiceProvider provider)
        {
            _mlContext = new MLContext(seed: 0);
            _logger = logger;                        
            _provider = provider;
        }

        private async Task<DocumentClassificationResult> PredictDocumentType(
            DocumentClassificationInput input,
            DocumentPatternResult patternResult)
        {
            await Task.CompletedTask; // placeholder

            var result = new DocumentClassificationResult();
            var scores = Enum.GetValues<DocumentType>()
                .ToDictionary(dt => dt, _ => 0.0);

            // Pattern-based scoring (40% weight)
            foreach (var confidence in patternResult.DocumentTypeConfidences)
                scores[confidence.Key] += confidence.Value * 0.4;

            // Specific pattern bonuses (30% weight)
            if (patternResult.HasAadhaarPattern) scores[DocumentType.Aadhaar] += 0.3;
            if (patternResult.HasPANPattern) scores[DocumentType.PAN] += 0.3;
            if (patternResult.HasPassportPattern) scores[DocumentType.Passport] += 0.3;

            // Filename-based scoring (15% weight)
            var fileName = input.FileName.ToLowerInvariant();
            if (fileName.Contains("aadhaar") || fileName.Contains("aadhar"))
                scores[DocumentType.Aadhaar] += 0.15;
            else if (fileName.Contains("pan"))
                scores[DocumentType.PAN] += 0.15;
            else if (fileName.Contains("passport"))
                scores[DocumentType.Passport] += 0.15;
            else if (fileName.Contains("license") || fileName.Contains("licence"))
                scores[DocumentType.DrivingLicense] += 0.15;
            else if (fileName.Contains("voter"))
                scores[DocumentType.VoterID] += 0.15;
            else if (fileName.Contains("ration"))
                scores[DocumentType.RationCard] += 0.15;
            else if (fileName.Contains("bank") || fileName.Contains("passbook"))
                scores[DocumentType.BankPassbook] += 0.15;
            else if (fileName.Contains("bill") || fileName.Contains("utility"))
                scores[DocumentType.UtilityBill] += 0.15;

            // Quality-based adjustment (15% weight)
            var qualityBonus = Math.Min(input.ImageQuality * 0.15, 0.15);
            var bestType = scores.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            scores[bestType] += qualityBonus;

            // Best match
            var bestMatch = scores.OrderByDescending(x => x.Value).First();
            result.PredictedType = bestMatch.Key;
            result.Confidence = Math.Min(bestMatch.Value, 1.0);
            result.AllPredictions = scores;

            // Processing notes
            var notes = new List<string>();
            if (input.TextConfidence < 0.7) notes.Add("Low OCR confidence");
            if (input.ImageQuality < 0.6) notes.Add("Poor image quality");
            if (patternResult.HasAadhaarPattern) notes.Add("Aadhaar number pattern detected");
            if (patternResult.HasPANPattern) notes.Add("PAN number pattern detected");
            if (patternResult.HasPassportPattern) notes.Add("Passport number pattern detected");

            result.ProcessingNotes = string.Join("; ", notes);

            return result;
        }

        private async Task CreateRuleBasedModel()
        {
            await Task.CompletedTask;
            _model = new DummyTransformer();
            _predictionEngine = new object(); // placeholder instead of PredictionEngine
            _logger.LogInformation("Rule-based classification model created");
        }

        private static long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return 0;
            }
        }

        public async Task InitializeModelAsync()
        {
            try
            {
                _logger.LogInformation("Initializing document classification model...");
                await CreateRuleBasedModel();
                _logger.LogInformation("Document classification model initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize document classification model");
                throw;
            }
        }

        public async Task<DocumentClassificationResult> ClassifyDocumentAsync(string filePath, string fileName = "")
        {
            try
            {
                if (!IsModelReady)
                    await InitializeModelAsync();

                _logger.LogInformation("Classifying document: {FilePath}", filePath);

                using var scope = _provider.CreateScope();
                var ocrService = scope.ServiceProvider.GetRequiredService<IOCRService>();
                var ocrResult = await ocrService.ExtractTextFromImageAsync(filePath);
                if (!ocrResult.Success)
                {
                    return new DocumentClassificationResult
                    {
                        PredictedType = DocumentType.Other,
                        Confidence = 0.0,
                        ProcessingNotes = $"OCR failed: {string.Join(", ", ocrResult.Errors)}"
                    };
                }

                var textPatternService = scope.ServiceProvider.GetRequiredService<ITextPatternService>();
                var patternResult = textPatternService.AnalyzeText(ocrResult.ExtractedText, fileName);
                var qualityResult = await ocrService.AnalyzeImageQualityAsync(filePath);

                var input = new DocumentClassificationInput
                {
                    ImagePath = filePath,
                    FileName = fileName,
                    FileSize = GetFileSize(filePath),
                    FileExtension = Path.GetExtension(filePath).ToLowerInvariant(),
                    ExtractedText = ocrResult.ExtractedText,
                    HasAadhaarPattern = patternResult.HasAadhaarPattern,
                    HasPANPattern = patternResult.HasPANPattern,
                    HasPassportPattern = patternResult.HasPassportPattern,
                    TextConfidence = ocrResult.Confidence,
                    ImageQuality = qualityResult.OverallQuality
                };

                var prediction = await PredictDocumentType(input, patternResult);

                _logger.LogInformation("Document classified as {DocumentType} with {Confidence}% confidence",
                    prediction.PredictedType, Math.Round(prediction.Confidence * 100, 1));

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying document {FilePath}", filePath);
                return new DocumentClassificationResult
                {
                    PredictedType = DocumentType.Other,
                    Confidence = 0.0,
                    ProcessingNotes = $"Classification error: {ex.Message}"
                };
            }
        }

        public void Dispose()
        {
            // No unmanaged resources to dispose
        }
    }

    // Dummy transformer for rule-based model
    internal class DummyTransformer : ITransformer
    {
        public DataViewSchema GetOutputSchema(DataViewSchema inputSchema) => inputSchema;
        public IDataView Transform(IDataView input) => input;
        public void Save(ModelSaveContext ctx) { }
        public bool IsRowToRowMapper => false;
        public IRowToRowMapper GetRowToRowMapper(DataViewSchema inputSchema) =>
            throw new NotImplementedException();
    }
}
