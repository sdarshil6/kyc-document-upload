using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class EnhancedOCRService : IEnhancedOCRService
    {
        private readonly ILogger<EnhancedOCRService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOCREngineFactory _engineFactory;
        private readonly OCRConfiguration _ocrConfig;

        public EnhancedOCRService(ILogger<EnhancedOCRService> logger, IConfiguration configuration, IOCREngineFactory engineFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _engineFactory = engineFactory;
            _ocrConfig = LoadOCRConfiguration();
        }

        public async Task<EnhancedOCRResult> ExtractTextAsync(string imagePath, OCRProcessingOptions? options = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                options ??= CreateDefaultProcessingOptions();

                _logger.LogInformation("Starting enhanced OCR processing for {ImagePath}", imagePath);

                var qualityMetrics = await AnalyzeImageQualityAsync(imagePath);              

                var result = new EnhancedOCRResult
                {
                    PrimaryEngine = OCREngine.Tesseract,
                    QualityMetrics = qualityMetrics,
                    ProcessingStats = new ProcessingStatistics()
                };

                var primaryEngineInstance = _engineFactory.CreateEngine();
                var primaryResult = await primaryEngineInstance.ExtractTextAsync(imagePath, options);
                result.EngineResults.Add(primaryResult);

                if (primaryResult.Success && primaryResult.Confidence >= options.MinimumConfidence)
                {
                    result.Success = true;
                    result.ExtractedText = primaryResult.ExtractedText;
                    result.OverallConfidence = primaryResult.Confidence;
                    result.ProcessingStats.PrimaryEngineTime = primaryResult.ProcessingTime;
                }
                //else if (options.EnableFallback)
                //{
                //    _logger.LogInformation("Primary engine failed or low confidence, trying fallback engine");

                //    var fallbackEngine = GetFallbackEngine(optimalEngine);
                //    result.FallbackEngine = fallbackEngine;

                //    var fallbackEngineInstance = _engineFactory.CreateEngine(fallbackEngine);
                //    var fallbackResult = await fallbackEngineInstance.ExtractTextAsync(imagePath, options);
                //    result.EngineResults.Add(fallbackResult);
                //    result.ProcessingStats.FallbackEngineTime = fallbackResult.ProcessingTime;
                //    result.ProcessingStats.UsedFallback = true;

                //    var bestResult = SelectBestResult(result.EngineResults);
                //    result.Success = bestResult.Success;
                //    result.ExtractedText = bestResult.ExtractedText;
                //    result.OverallConfidence = bestResult.Confidence;
                //}

                if (result.Success && !string.IsNullOrEmpty(result.ExtractedText))
                {
                    result.TextAnalysis = AnalyzeExtractedText(result.ExtractedText, result.EngineResults);
                    result.DetectedLanguages = DetectLanguages(result.ExtractedText);
                }

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.ProcessingStats.TotalProcessingTime = stopwatch.Elapsed;

                result.Metadata = CreateMetadata(result, options);

                _logger.LogInformation("Enhanced OCR completed in {ProcessingTime}ms. Success: {Success}, Confidence: {Confidence}%",
                    stopwatch.ElapsedMilliseconds, result.Success, Math.Round(result.OverallConfidence * 100, 1));

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Enhanced OCR processing failed for {ImagePath}", imagePath);

                return new EnhancedOCRResult
                {
                    Success = false,
                    ExtractedText = string.Empty,
                    OverallConfidence = 0f,
                    ProcessingTime = stopwatch.Elapsed,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<EnhancedOCRResult> ExtractTextAsync(Stream imageStream, string fileName, OCRProcessingOptions? options = null)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                return await ExtractTextAsync(tempPath, options);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public async Task<ImageQualityMetrics> AnalyzeImageQualityAsync(string imagePath)
        {
            try
            {
                var fileInfo = new FileInfo(imagePath);

                var random = new Random();

                var metrics = new ImageQualityMetrics
                {
                    Brightness = 0.3f + (float)(random.NextDouble() * 0.4),
                    Contrast = 0.4f + (float)(random.NextDouble() * 0.4),
                    Sharpness = 0.5f + (float)(random.NextDouble() * 0.4),
                    NoiseLevel = (float)(random.NextDouble() * 0.3),
                    Resolution = 300f
                };

                metrics.OverallQuality = (metrics.Brightness +
                                         metrics.Contrast +
                                         metrics.Sharpness +
                                         (1 - metrics.NoiseLevel)) / 4f;

                metrics.IsBlurry = metrics.Sharpness < 0.6f;
                metrics.IsTooDark = metrics.Brightness < 0.4f;
                metrics.IsTooLight = metrics.Brightness > 0.8f;
                metrics.HasSufficientContrast = metrics.Contrast >= 0.5f;

                if (metrics.IsBlurry)
                    metrics.QualityIssues.Add("Image appears blurry");

                if (metrics.IsTooDark)
                    metrics.QualityIssues.Add("Image is too dark");

                if (metrics.IsTooLight)
                    metrics.QualityIssues.Add("Image is overexposed");

                if (metrics.NoiseLevel > 0.2f)
                    metrics.QualityIssues.Add("High noise level detected");

                if (!metrics.HasSufficientContrast)
                    metrics.QualityIssues.Add("Low contrast detected");

                if (metrics.QualityIssues.Count == 0)
                    metrics.Recommendations.Add("Image quality is good for OCR");
                else
                    metrics.Recommendations.Add("Consider image preprocessing to improve OCR accuracy");

                await Task.CompletedTask;
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image quality for {ImagePath}", imagePath);

                return new ImageQualityMetrics
                {
                    OverallQuality = 0.5f,
                    QualityIssues = new List<string> { "Quality analysis failed" },
                    Recommendations = new List<string> { "Manual quality check recommended" }
                };
            }
        }

        public async Task<ImageQualityMetrics> AnalyzeImageQualityAsync(Stream imageStream)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                return await AnalyzeImageQualityAsync(tempPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public async Task<List<OCREngineStatus>> GetEngineStatusAsync()
        {
            var statuses = new List<OCREngineStatus>();

            foreach (var engineType in Enum.GetValues<OCREngine>())
            {
                if (engineType == OCREngine.Hybrid) continue;

                try
                {
                    var engine = _engineFactory.CreateEngine();
                    var status = await engine.GetStatusAsync();
                    statuses.Add(status);
                }
                catch (Exception ex)
                {
                    statuses.Add(new OCREngineStatus
                    {
                        Engine = engineType,
                        IsAvailable = false,
                        IsHealthy = false,
                        StatusMessage = $"Error: {ex.Message}",
                        LastHealthCheck = DateTime.UtcNow
                    });
                }
            }

            return statuses;
        }

        public async Task<List<OCREngineCapabilities>> GetEngineCapabilitiesAsync()
        {            
            try
            {
                var capabilities = new List<OCREngineCapabilities>();
                var engine = _engineFactory.CreateEngine();
                var capability = await engine.GetCapabilitiesAsync();
                capabilities.Add(capability);
                return capabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetEngineCapabilitiesAsync() in EnhancedOCRService.cs : " + ex);
                throw;
            }            
        }

        public async Task<List<EnhancedOCRResult>> ProcessBatchAsync(List<string> imagePaths, OCRProcessingOptions? options = null)
        {
            var results = new List<EnhancedOCRResult>();
            var semaphore = new SemaphoreSlim(_ocrConfig.MaxConcurrentProcesses);

            var tasks = imagePaths.Select(async imagePath =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ExtractTextAsync(imagePath, options);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            results.AddRange(await Task.WhenAll(tasks));
            return results;
        }

        public async Task<Dictionary<string, object>> GetPerformanceMetricsAsync()
        {
            var engineStatuses = await GetEngineStatusAsync();

            return new Dictionary<string, object>
            {
                { "TotalEngines", engineStatuses.Count },
                { "HealthyEngines", engineStatuses.Count(s => s.IsHealthy) },
                { "AvailableEngines", engineStatuses.Count(s => s.IsAvailable) },
                { "EngineStatuses", engineStatuses.ToDictionary(s => s.Engine.ToString(), s => new
                    {
                        s.IsHealthy,
                        s.IsAvailable,
                        s.SuccessRate,
                        AverageResponseTimeMs = s.AverageResponseTime.TotalMilliseconds
                    })
                },
                { "SystemInfo", new
                    {
                        ProcessorCount = Environment.ProcessorCount,
                        MachineName = Environment.MachineName,
                        OSVersion = Environment.OSVersion.ToString(),
                        WorkingSetMB = GC.GetTotalMemory(false) / (1024 * 1024)
                    }
                }
            };
        }

        private OCRConfiguration LoadOCRConfiguration()
        {
            return new OCRConfiguration
            {
                TesseractPath = _configuration["OCRSettings:TesseractPath"] ?? "tesseract",
                TesseractDataPath = _configuration["OCRSettings:TesseractDataPath"] ?? "",               
                DefaultLanguages = _configuration.GetSection("OCRSettings:DefaultLanguages").Get<List<string>>() ?? new List<string> { "eng", "hin", "guj" },
                ProcessingTimeout = _configuration.GetValue<int>("OCRSettings:ProcessingTimeout", 30000),
                MaxRetries = _configuration.GetValue<int>("OCRSettings:MaxRetries", 3),
                PreprocessImages = _configuration.GetValue<bool>("OCRSettings:PreprocessImages", true),
                EnableParallelProcessing = _configuration.GetValue<bool>("OCRSettings:EnableParallelProcessing", true),
                CacheResults = _configuration.GetValue<bool>("OCRSettings:CacheResults", true),
                MaxConcurrentProcesses = _configuration.GetValue<int>("OCRSettings:MaxConcurrentProcesses", Environment.ProcessorCount)
            };
        }

        private OCRProcessingOptions CreateDefaultProcessingOptions()
        {
            return new OCRProcessingOptions
            {
                Languages = _ocrConfig.DefaultLanguages,
                PreferredEngine = OCREngine.Tesseract,
                EnableFallback = true,
                PreprocessImage = _ocrConfig.PreprocessImages,
                AnalyzeQuality = true,
                ExtractWordDetails = false,
                TimeoutSeconds = _ocrConfig.ProcessingTimeout / 1000,
                MaxRetries = _ocrConfig.MaxRetries,
                MinimumConfidence = 0.5f
            };
        }

        private OCREngine GetFallbackEngine(OCREngine primaryEngine)
        {
            return primaryEngine switch
            {
                OCREngine.Tesseract => OCREngine.EasyOCR,
                OCREngine.EasyOCR => OCREngine.Tesseract,
                _ => OCREngine.Tesseract
            };
        }

        private EngineResult SelectBestResult(List<EngineResult> results)
        {
            if (!results.Any())
                throw new ArgumentException("No results to select from");

            var successfulResults = results.Where(r => r.Success).ToList();

            if (!successfulResults.Any())
                return results.First();

            return successfulResults.OrderByDescending(r => r.Confidence).First();
        }

        private TextAnalysisResult AnalyzeExtractedText(string text, List<EngineResult> engineResults)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var analysis = new TextAnalysisResult
            {
                TotalCharacters = text.Length,
                TotalWords = words.Length,
                TotalLines = lines.Length,
                AverageWordConfidence = engineResults.Where(r => r.Success).Average(r => r.Confidence),
                HasNumbers = text.Any(char.IsDigit),
                HasDates = System.Text.RegularExpressions.Regex.IsMatch(text, @"\d{1,2}[/\-]\d{1,2}[/\-]\d{4}"),
                HasEmails = System.Text.RegularExpressions.Regex.IsMatch(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"),
                HasPhoneNumbers = System.Text.RegularExpressions.Regex.IsMatch(text, @"\b\d{10}\b"),
                Complexity = DetermineTextComplexity(text, engineResults)
            };

            // Detect patterns
            var patterns = new List<string>();
            if (analysis.HasNumbers) patterns.Add("Numbers");
            if (analysis.HasDates) patterns.Add("Dates");
            if (analysis.HasEmails) patterns.Add("Email addresses");
            if (analysis.HasPhoneNumbers) patterns.Add("Phone numbers");

            // Check for Indian document patterns
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b\d{4}\s?\d{4}\s?\d{4}\b"))
                patterns.Add("Aadhaar number pattern");

            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b[A-Z]{5}\d{4}[A-Z]\b"))
                patterns.Add("PAN number pattern");

            analysis.DetectedPatterns = patterns;

            return analysis;
        }

        private TextComplexity DetermineTextComplexity(string text, List<EngineResult> engineResults)
        {
            var averageConfidence = engineResults.Where(r => r.Success).Average(r => r.Confidence);

            if (averageConfidence >= 0.9f) return TextComplexity.Simple;
            if (averageConfidence >= 0.7f) return TextComplexity.Moderate;
            if (averageConfidence >= 0.5f) return TextComplexity.Complex;

            return TextComplexity.Poor;
        }

        private List<string> DetectLanguages(string text)
        {
            var languages = new List<string>();

            if (text.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                languages.Add("en");

            if (text.Any(c => c >= 0x0900 && c <= 0x097F)) // Devanagari range
                languages.Add("hi");

            return languages.Any() ? languages : new List<string> { "unknown" };
        }

        private Dictionary<string, object> CreateMetadata(EnhancedOCRResult result, OCRProcessingOptions options)
        {
            return new Dictionary<string, object>
            {
                { "ProcessingMode", "Enhanced" },
                { "PrimaryEngine", result.PrimaryEngine.ToString() },
                { "FallbackEngine", result.FallbackEngine?.ToString() ?? "None" },
                { "UsedFallback", result.ProcessingStats.UsedFallback },
                { "EnginesUsed", result.EngineResults.Count },
                { "Languages", options.Languages },
                { "PreprocessingEnabled", options.PreprocessImage },
                { "QualityAnalysisEnabled", options.AnalyzeQuality },
                { "ProcessedAt", result.ProcessedAt },
                { "MachineName", Environment.MachineName },
                { "Version", "1.0.0" }
            };
        }
    }
}
