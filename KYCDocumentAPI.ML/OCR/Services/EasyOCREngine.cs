/*
using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class EasyOCREngine : IOCREngine
    {
        private readonly ILogger<EasyOCREngine> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private bool _isInitialized;
        private OCREngineStatus _engineStatus;

        public EasyOCREngine(ILogger<EasyOCREngine> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _pythonPath = _configuration["OCRSettings:PythonPath"] ?? "python";
            _scriptPath = _configuration["OCRSettings:EasyOCRScriptPath"] ?? "Scripts/easyocr_processor.py";

            _engineStatus = new OCREngineStatus
            {
                Engine = EngineType,
                IsAvailable = false,
                IsHealthy = false,
                StatusMessage = "Not initialized",
                LastHealthCheck = DateTime.UtcNow
            };
        }

        public OCREngine EngineType => OCREngine.EasyOCR;

        public async Task DisposeAsync()
        {
            try
            {
                _isInitialized = false;
                _engineStatus.IsAvailable = false;
                _engineStatus.StatusMessage = "Disposed";

                await Task.CompletedTask;
                _logger.LogInformation("EasyOCR engine disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing EasyOCR engine");
            }
        }

        public async Task<EngineResult> ExtractTextAsync(string imagePath, OCRProcessingOptions options)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                _logger.LogInformation("Starting EasyOCR processing for {ImagePath}", imagePath);

                // Validate input
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException($"Image file not found: {imagePath}");
                }

                // Prepare Python script arguments
                var arguments = BuildPythonArguments(imagePath, options);

                // Execute Python script
                var result = await ExecutePythonScriptAsync(arguments, options.TimeoutSeconds * 1000);

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogInformation("EasyOCR completed in {ProcessingTime}ms. Success: {Success}, Confidence: {Confidence}%",
                    stopwatch.ElapsedMilliseconds, result.Success, Math.Round(result.Confidence * 100, 1));

                // Update success statistics
                if (result.Success)
                    _engineStatus.SuccessfulRequests++;
                else
                    _engineStatus.FailedRequests++;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "EasyOCR processing failed for {ImagePath}", imagePath);

                _engineStatus.FailedRequests++;

                return new EngineResult
                {
                    Engine = EngineType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<EngineResult> ExtractTextAsync(Stream imageStream, OCRProcessingOptions options)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                // Save stream to temporary file
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

        public async Task<OCREngineCapabilities> GetCapabilitiesAsync()
        {
            try
            {
                var arguments = "capabilities";
                var result = await ExecutePythonScriptAsync(arguments, 10000); // 10 second timeout

                if (result.Success && result.EngineSpecificData.ContainsKey("capabilities"))
                {
                    // Parse capabilities from Python script output
                    var capabilitiesJson = result.EngineSpecificData["capabilities"]?.ToString();
                    if (!string.IsNullOrEmpty(capabilitiesJson))
                    {
                        var capabilities = JsonSerializer.Deserialize<Dictionary<string, object>>(capabilitiesJson);

                        return new OCREngineCapabilities
                        {
                            Engine = EngineType,
                            Version = capabilities?.GetValueOrDefault("version", "Unknown").ToString() ?? "Unknown",
                            SupportedLanguages = ExtractLanguageList(capabilities),
                            SupportedFormats = new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" },
                            SupportsWordDetails = true,
                            SupportsConfidenceScores = true,
                            SupportsMultipleLanguages = true,
                            SupportsHandwriting = true,
                            AverageAccuracy = 0.90f,
                            AverageSpeed = 1.5f,
                            AdditionalCapabilities = new Dictionary<string, object>
                            {
                                { "NeuralNetworkBased", true },
                                { "HandwritingRecognition", true },
                                { "MultiLanguageSupport", true },
                                { "GPUAcceleration", true },
                                { "PreprocessingBuiltIn", true }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get EasyOCR capabilities");
            }

            // Return default capabilities if script fails
            return new OCREngineCapabilities
            {
                Engine = EngineType,
                Version = "Unknown",
                SupportedLanguages = new List<string> { "eng", "hin", "guj" },
                SupportedFormats = new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" },
                SupportsWordDetails = true,
                SupportsConfidenceScores = true,
                SupportsMultipleLanguages = true,
                SupportsHandwriting = true,
                AverageAccuracy = 0.90f,
                AverageSpeed = 1.5f
            };
        }

        public async Task<OCREngineStatus> GetStatusAsync()
        {
            await Task.CompletedTask;
            _engineStatus.LastHealthCheck = DateTime.UtcNow;
            return _engineStatus;
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                _logger.LogDebug("Performing EasyOCR health check");
                
                var arguments = "health";
                var result = await ExecutePythonScriptAsync(arguments, 15000); // 15 second timeout

                var isHealthy = result.Success;

                _engineStatus.IsHealthy = isHealthy;
                _engineStatus.IsAvailable = isHealthy;
                _engineStatus.StatusMessage = isHealthy ? "Healthy" : $"Health check failed: {result.ErrorMessage}";
                _engineStatus.LastHealthCheck = DateTime.UtcNow;

                _logger.LogInformation("EasyOCR health check result: {IsHealthy}", isHealthy);

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EasyOCR health check failed");
                _engineStatus.IsHealthy = false;
                _engineStatus.IsAvailable = false;
                _engineStatus.StatusMessage = $"Health check error: {ex.Message}";
                return false;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing EasyOCR engine");

                // Verify Python installation
                if (!await VerifyPythonInstallationAsync())
                {
                    throw new InvalidOperationException("Python installation not found or not accessible");
                }

                // Verify script exists
                if (!File.Exists(_scriptPath))
                {
                    throw new FileNotFoundException($"EasyOCR script not found: {_scriptPath}");
                }

                // Test script execution
                var healthResult = await HealthCheckAsync();
                if (!healthResult)
                {
                    throw new InvalidOperationException("EasyOCR script failed initial health check");
                }

                _isInitialized = true;
                _engineStatus.IsAvailable = true;
                _engineStatus.StatusMessage = "Initialized successfully";

                _logger.LogInformation("EasyOCR engine initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize EasyOCR engine");
                _engineStatus.StatusMessage = $"Initialization failed: {ex.Message}";
                throw;
            }
        }

        private string BuildPythonArguments(string imagePath, OCRProcessingOptions options)
        {
            var arguments = new List<string>
            {
                "process",
                "--image", $"\"{imagePath}\"",
                "--languages"
            };

            arguments.AddRange(options.Languages);

            if (options.MinimumConfidence > 0)
            {
                arguments.Add("--confidence");
                arguments.Add(options.MinimumConfidence.ToString("F2"));
            }

            if (options.PreprocessImage)
            {
                arguments.Add("--preprocess");
            }

            if (options.ExtractWordDetails)
            {
                arguments.Add("--word-details");
            }

            // Add GPU flag if configured
            var useGpu = _configuration.GetValue<bool>("OCRSettings:EasyOCR:UseGPU", false);
            if (useGpu)
            {
                arguments.Add("--gpu");
            }

            return string.Join(" ", arguments);
        }

        private async Task<EngineResult> ExecutePythonScriptAsync(string arguments, int timeoutMs)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_scriptPath}\" {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                _logger.LogDebug("Executing Python script: {FileName} {Arguments}", _pythonPath, process.StartInfo.Arguments);

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                var completed = await Task.WhenAny(
                    Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync()),
                    Task.Delay(timeoutMs)
                );

                if (completed == Task.Delay(timeoutMs))
                {
                    // Timeout occurred
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Failed to kill timed-out Python process");
                    }

                    throw new TimeoutException($"Python script execution timed out after {timeoutMs}ms");
                }

                var output = await outputTask;
                var error = await errorTask;

                _logger.LogDebug("Python script output: {Output}", output);
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogDebug("Python script stderr: {Error}", error);
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Python script failed with exit code {process.ExitCode}. Error: {error}");
                }

                // Parse JSON output
                return ParsePythonOutput(output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute Python script");

                return new EngineResult
                {
                    Engine = EngineType,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private EngineResult ParsePythonOutput(string output)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(output))
                {
                    throw new InvalidOperationException("Empty output from Python script");
                }

                var jsonResult = JsonSerializer.Deserialize<Dictionary<string, object>>(output);

                if (jsonResult == null)
                {
                    throw new InvalidOperationException("Failed to parse Python script output as JSON");
                }

                var success = jsonResult.GetValueOrDefault("success", false);

                if (success is JsonElement successElement && successElement.ValueKind == JsonValueKind.True)
                {
                    // Parse successful result
                    var extractedText = jsonResult.GetValueOrDefault("extracted_text", "").ToString() ?? "";
                    var confidence = ParseFloat(jsonResult.GetValueOrDefault("confidence", 0.0f));
                    var wordDetails = ParseWordDetails(jsonResult.GetValueOrDefault("word_details", new object[0]));

                    return new EngineResult
                    {
                        Engine = EngineType,
                        Success = true,
                        ExtractedText = extractedText,
                        Confidence = confidence,
                        WordDetails = wordDetails,
                        EngineSpecificData = jsonResult.Where(kvp => kvp.Key != "word_details")
                                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };
                }
                else
                {
                    // Parse error result
                    var errorMessage = jsonResult.GetValueOrDefault("error", "Unknown error").ToString() ?? "Unknown error";

                    return new EngineResult
                    {
                        Engine = EngineType,
                        Success = false,
                        ErrorMessage = errorMessage,
                        EngineSpecificData = jsonResult
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Python output: {Output}", output);

                return new EngineResult
                {
                    Engine = EngineType,
                    Success = false,
                    ErrorMessage = $"Failed to parse Python output: {ex.Message}",
                    EngineSpecificData = new Dictionary<string, object> { { "raw_output", output } }
                };
            }
        }

        private List<WordDetail> ParseWordDetails(object wordDetailsObj)
        {
            var wordDetails = new List<WordDetail>();

            try
            {
                if (wordDetailsObj is JsonElement wordDetailsElement && wordDetailsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var wordElement in wordDetailsElement.EnumerateArray())
                    {
                        if (wordElement.ValueKind == JsonValueKind.Object)
                        {
                            var wordDict = JsonSerializer.Deserialize<Dictionary<string, object>>(wordElement.GetRawText());
                            if (wordDict != null)
                            {
                                var wordDetail = new WordDetail
                                {
                                    Text = wordDict.GetValueOrDefault("text", "").ToString() ?? "",
                                    Confidence = ParseFloat(wordDict.GetValueOrDefault("confidence", 0.0f)),
                                    IsNumeric = ParseBool(wordDict.GetValueOrDefault("is_numeric", false)),
                                    IsAlphabetic = ParseBool(wordDict.GetValueOrDefault("is_alphabetic", false)),
                                    Language = wordDict.GetValueOrDefault("language", "en").ToString() ?? "en"
                                };

                                // Parse bounding box
                                if (wordDict.ContainsKey("bounding_box"))
                                {
                                    var bboxObj = wordDict["bounding_box"];
                                    if (bboxObj is JsonElement bboxElement && bboxElement.ValueKind == JsonValueKind.Object)
                                    {
                                        var bboxDict = JsonSerializer.Deserialize<Dictionary<string, object>>(bboxElement.GetRawText());
                                        if (bboxDict != null)
                                        {
                                            wordDetail.BoundingBox = new BoundingBox
                                            {
                                                X = ParseInt(bboxDict.GetValueOrDefault("x", 0)),
                                                Y = ParseInt(bboxDict.GetValueOrDefault("y", 0)),
                                                Width = ParseInt(bboxDict.GetValueOrDefault("width", 0)),
                                                Height = ParseInt(bboxDict.GetValueOrDefault("height", 0))
                                            };
                                        }
                                    }
                                }

                                wordDetails.Add(wordDetail);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse word details from Python output");
            }

            return wordDetails;
        }

        private float ParseFloat(object value)
        {
            try
            {
                if (value is JsonElement element)
                {
                    return element.GetSingle();
                }
                return Convert.ToSingle(value);
            }
            catch
            {
                return 0.0f;
            }
        }

        private int ParseInt(object value)
        {
            try
            {
                if (value is JsonElement element)
                {
                    return element.GetInt32();
                }
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private bool ParseBool(object value)
        {
            try
            {
                if (value is JsonElement element)
                {
                    return element.GetBoolean();
                }
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        private List<string> ExtractLanguageList(Dictionary<string, object>? capabilities)
        {
            try
            {
                if (capabilities?.ContainsKey("supported_languages") == true)
                {
                    var languagesObj = capabilities["supported_languages"];
                    if (languagesObj is JsonElement languagesElement && languagesElement.ValueKind == JsonValueKind.Array)
                    {
                        return languagesElement.EnumerateArray()
                            .Select(lang => lang.GetString() ?? "")
                            .Where(lang => !string.IsNullOrEmpty(lang))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract language list from capabilities");
            }

            return new List<string> { "en", "hi" }; // Default fallback
        }

        private async Task<bool> VerifyPythonInstallationAsync()
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0 && output.ToLowerInvariant().Contains("python");

                _logger.LogDebug("Python verification result: {Success}, Output: {Output}", success, output.Trim());

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify Python installation");
                return false;
            }
        }
    }
}
*/