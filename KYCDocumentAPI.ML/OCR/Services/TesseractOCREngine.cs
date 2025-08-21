using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Tesseract;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class TesseractOCREngine : IOCREngine
    {
        private readonly ILogger<TesseractOCREngine> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tesseractPath;
        private readonly string _tesseractDataPath;
        private bool _isInitialized;
        private OCREngineStatus _engineStatus;

        public OCREngine EngineType => OCREngine.Tesseract;

        public TesseractOCREngine(ILogger<TesseractOCREngine> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _tesseractPath = _configuration["OCRSettings:TesseractPath"] ?? "tesseract";
            _tesseractDataPath = _configuration["OCRSettings:TesseractDataPath"] ?? "";

            _engineStatus = new OCREngineStatus
            {
                Engine = EngineType,
                IsAvailable = false,
                IsHealthy = false,
                StatusMessage = "Not initialized",
                LastHealthCheck = DateTime.UtcNow
            };
        }

        public async Task<EngineResult> ExtractTextAsync(string imagePath, OCRProcessingOptions options)
        {
            var stopwatch = Stopwatch.StartNew();
            string processedImagePath = string.Empty;
            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                _logger.LogInformation("Starting Tesseract OCR processing for {ImagePath}", imagePath);

                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"Image file not found: {imagePath}");

                processedImagePath = imagePath;
                if (options.PreprocessImage)
                {
                    processedImagePath = await PreprocessImageAsync(imagePath, options.PreprocessingOptions);
                }

                var result = await PerformTesseractOCRAsync(processedImagePath, options);

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogInformation("Tesseract OCR completed in {ProcessingTime}ms. Confidence: {Confidence}%",
                    stopwatch.ElapsedMilliseconds, Math.Round(result.Confidence * 100, 1));

                _engineStatus.SuccessfulRequests++;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Tesseract OCR processing failed for {ImagePath}", imagePath);
                _engineStatus.FailedRequests++;

                return new EngineResult
                {
                    Engine = EngineType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            finally
            {
                if (processedImagePath != imagePath && File.Exists(processedImagePath))
                {
                    try { File.Delete(processedImagePath); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to cleanup preprocessed file: {FilePath}", processedImagePath); }
                }
            }
        }

        public async Task<EngineResult> ExtractTextAsync(Stream imageStream, OCRProcessingOptions options)
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
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        public async Task<OCREngineStatus> GetStatusAsync()
        {
            await Task.CompletedTask;
            _engineStatus.LastHealthCheck = DateTime.UtcNow;
            return _engineStatus;
        }

        public async Task<OCREngineCapabilities> GetCapabilitiesAsync()
        {
            await Task.CompletedTask;
            return new OCREngineCapabilities
            {
                Engine = EngineType,
                Version = await GetTesseractVersionAsync(),
                SupportedLanguages = await GetSupportedLanguagesAsync(),
                SupportedFormats = new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".pdf" },
                SupportsWordDetails = true,
                SupportsConfidenceScores = true,
                SupportsMultipleLanguages = true,
                SupportsHandwriting = false,
                AverageAccuracy = 0.85f,
                AverageSpeed = 0.5f,
                AdditionalCapabilities = new Dictionary<string, object>
                {
                    { "PageSegmentationModes", Enum.GetNames<PageSegmentationMode>() },
                    { "OCREngineModes", Enum.GetNames<OCREngineMode>() },
                    { "CustomVariables", true },
                    { "ConfigFiles", true }
                }
            };
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var versionResult = await RunTesseractCommandAsync("--version");
                if (!versionResult.Success)
                {
                    _engineStatus.IsHealthy = false;
                    _engineStatus.StatusMessage = "Tesseract executable not accessible";
                    return false;
                }

                var testResult = await PerformHealthCheckWithTestImageAsync();
                _engineStatus.IsHealthy = testResult;
                _engineStatus.IsAvailable = testResult;
                _engineStatus.StatusMessage = testResult ? "Healthy" : "Health check failed";
                _engineStatus.LastHealthCheck = DateTime.UtcNow;
                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract health check failed");
                _engineStatus.IsHealthy = false;
                _engineStatus.StatusMessage = $"Health check error: {ex.Message}";
                return false;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Tesseract OCR engine");

                var versionResult = await RunTesseractCommandAsync("--version");
                if (!versionResult.Success) throw new InvalidOperationException("Tesseract executable not found");

                var languageResult = await RunTesseractCommandAsync("--list-langs");
                if (!languageResult.Success) throw new InvalidOperationException("Failed to retrieve Tesseract languages");

                var availableLanguages = ParseLanguageList(languageResult.Output);
                if (!availableLanguages.Contains("eng"))
                    throw new InvalidOperationException("English language pack not found");

                _isInitialized = true;
                _engineStatus.IsAvailable = true;
                _engineStatus.StatusMessage = "Initialized successfully";

                _logger.LogInformation("Tesseract initialized. Languages: {Languages}", string.Join(", ", availableLanguages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tesseract OCR engine");
                _engineStatus.StatusMessage = $"Initialization failed: {ex.Message}";
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            _isInitialized = false;
            _engineStatus.IsAvailable = false;
            _engineStatus.StatusMessage = "Disposed";
            await Task.CompletedTask;
            _logger.LogInformation("Tesseract OCR engine disposed");
        }

        private async Task<EngineResult> PerformTesseractOCRAsync(string imagePath, OCRProcessingOptions options)
        {
            try
            {
                using var engine = new Tesseract.TesseractEngine(_tesseractDataPath, string.Join("+", options.Languages), EngineMode.Default);

                ConfigureTesseractEngine(engine, options);

                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img);

                var extractedText = page.GetText().Trim();
                var confidence = page.GetMeanConfidence();

                var wordDetails = options.ExtractWordDetails ? ExtractWordLevelDetails(page) : new List<WordDetail>();

                return new EngineResult
                {
                    Engine = EngineType,
                    Success = true,
                    ExtractedText = CleanExtractedText(extractedText),
                    Confidence = confidence,
                    WordDetails = wordDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract processing failed for {ImagePath}", imagePath);
                return new EngineResult { Engine = EngineType, Success = false, ErrorMessage = ex.Message };
            }
        }

        private void ConfigureTesseractEngine(Tesseract.TesseractEngine engine, OCRProcessingOptions options)
        {
            engine.SetVariable("preserve_interword_spaces", "1");
            engine.SetVariable("user_defined_dpi", "300");
            if (options.Languages.Contains("hi"))
            {
                engine.SetVariable("textord_really_old_xheight", "1");
                engine.SetVariable("textord_min_xheight", "10");
            }
        }

        private List<WordDetail> ExtractWordLevelDetails(Tesseract.Page page)
        {
            var wordDetails = new List<WordDetail>();
            using var iter = page.GetIterator();
            iter.Begin();
            do
            {
                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                {
                    var word = iter.GetText(PageIteratorLevel.Word)?.Trim();
                    var confidence = iter.GetConfidence(PageIteratorLevel.Word) / 100f;
                    if (!string.IsNullOrEmpty(word))
                    {
                        wordDetails.Add(new WordDetail
                        {
                            Text = word,
                            Confidence = confidence,
                            BoundingBox = new BoundingBox
                            {
                                X = bounds.X1,
                                Y = bounds.Y1,
                                Width = bounds.X2 - bounds.X1,
                                Height = bounds.Y2 - bounds.Y1
                            }
                        });
                    }
                }
            } while (iter.Next(PageIteratorLevel.Word));
            return wordDetails;
        }

        private async Task<string> PreprocessImageAsync(string imagePath, ImagePreprocessingOptions options)
        {
            try
            {
                var preprocessedPath = Path.GetTempFileName() + Path.GetExtension(imagePath);
                using var image = await Image.LoadAsync(imagePath);

                image.Mutate(x =>
                {
                    if (options.AutoRotate) x.AutoOrient();
                    if (options.EnhanceContrast) x.Contrast(options.ContrastFactor);
                    if (options.NormalizeSize && (image.Width > options.MaxWidth || image.Height > options.MaxHeight))
                    {
                        x.Resize(new ResizeOptions
                        {
                            Size = new Size(options.MaxWidth, options.MaxHeight),
                            Mode = ResizeMode.Max
                        });
                    }
                    x.Grayscale();
                });

                await image.SaveAsync(preprocessedPath);
                return preprocessedPath;
            }
            catch
            {
                return imagePath;
            }
        }

        private string CleanExtractedText(string text) =>
            Regex.Replace(text ?? "", @"\s+", " ").Replace("�", "").Replace("\u00A0", " ").Trim();

        private async Task<(bool Success, string Output)> RunTesseractCommandAsync(string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _tesseractPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode == 0, output + error);
        }

        private async Task<string> GetTesseractVersionAsync()
        {
            var result = await RunTesseractCommandAsync("--version");
            if (result.Success)
            {
                var lines = result.Output.Split('\n');
                var versionLine = lines.FirstOrDefault(l => l.Contains("tesseract"));
                return versionLine?.Trim() ?? "Unknown";
            }
            return "Unknown";
        }

        private async Task<List<string>> GetSupportedLanguagesAsync()
        {
            var result = await RunTesseractCommandAsync("--list-langs");
            return result.Success ? ParseLanguageList(result.Output) : new List<string> { "eng" };
        }

        private List<string> ParseLanguageList(string output) =>
            output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        private async Task<bool> PerformHealthCheckWithTestImageAsync()
        {
            var testImagePath = Path.GetTempFileName() + ".png";
            await CreateTestImageAsync(testImagePath);
            var result = await PerformTesseractOCRAsync(testImagePath, new OCRProcessingOptions
            {
                Languages = new List<string> { "eng" },
                TimeoutSeconds = 10,
                PreprocessImage = false
            });
            if (File.Exists(testImagePath)) File.Delete(testImagePath);
            return result.Success && !string.IsNullOrEmpty(result.ExtractedText);
        }

        private async Task CreateTestImageAsync(string imagePath)
        {
            using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(200, 100);
            image.Mutate(x => x
                .Fill(Color.White)
                .DrawText("TEST", SystemFonts.CreateFont("Arial", 24), SixLabors.ImageSharp.Color.Black, new PointF(50, 35)));
            await image.SaveAsync(imagePath);
        }
    }
}
