using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.ML.OCR.Services
{
    public class OCREngineFactory : IOCREngineFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OCREngineFactory> _logger;
        private readonly OCRConfiguration _configuration;
        private readonly Dictionary<OCREngine, IOCREngine> _engineInstances;
        private readonly Dictionary<OCREngine, DateTime> _lastHealthChecks;

        public OCREngineFactory(IServiceProvider serviceProvider, ILogger<OCREngineFactory> logger, OCRConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _engineInstances = new Dictionary<OCREngine, IOCREngine>();
            _lastHealthChecks = new Dictionary<OCREngine, DateTime>();
        }

        public IOCREngine CreateEngine(OCREngine engineType)
        {
            try
            {
                if (_engineInstances.ContainsKey(engineType))
                {
                    var existingEngine = _engineInstances[engineType];
                    if (ShouldReuseEngine(engineType))
                    {
                        return existingEngine;
                    }
                    else
                    {
                        _ = Task.Run(async () => await existingEngine.DisposeAsync());
                        _engineInstances.Remove(engineType);
                    }
                }

                IOCREngine engine = engineType switch
                {
                    OCREngine.EasyOCR => _serviceProvider.GetRequiredService<EasyOCREngine>(),
                    OCREngine.Tesseract => _serviceProvider.GetRequiredService<TesseractEngine>(),
                    _ => throw new NotSupportedException($"OCR engine {engineType} is not supported")
                };

                _engineInstances[engineType] = engine;
                _lastHealthChecks[engineType] = DateTime.UtcNow;

                _logger.LogInformation("Created new {EngineType} OCR engine instance", engineType);
                return engine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create {EngineType} OCR engine", engineType);
                throw;
            }
        }

        public List<IOCREngine> GetAvailableEngines()
        {
            try
            {
                var availableEngines = new List<IOCREngine>();

                foreach (var engineType in Enum.GetValues<OCREngine>())
                {
                    if (engineType == OCREngine.Hybrid) continue;

                    try
                    {
                        var engine = CreateEngine(engineType);
                        availableEngines.Add(engine);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Engine {EngineType} is not available", engineType);
                    }
                }

                return availableEngines;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetAvailableEngines() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task<List<OCREngine>> GetHealthyEnginesAsync()
        {
            try
            {
                var healthyEngines = new List<OCREngine>();

                foreach (var engineType in Enum.GetValues<OCREngine>())
                {
                    if (engineType == OCREngine.Hybrid) continue;

                    try
                    {
                        var engine = CreateEngine(engineType);
                        if (await engine.HealthCheckAsync())
                        {
                            healthyEngines.Add(engineType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Health check failed for {EngineType}", engineType);
                    }
                }

                _logger.LogInformation("Found {HealthyEngineCount} healthy OCR engines: {HealthyEngines}", healthyEngines.Count, string.Join(", ", healthyEngines));

                return healthyEngines;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetHealthyEnginesAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task<OCREngine> GetOptimalEngineAsync(string documentType, ImageQualityMetrics quality)
        {
            try
            {
                var healthyEngines = await GetHealthyEnginesAsync();

                if (!healthyEngines.Any())
                {
                    throw new InvalidOperationException("No healthy OCR engines available");
                }

                var documentPreferences = GetDocumentTypePreferences(documentType);

                foreach (var preferredEngine in documentPreferences)
                {
                    if (healthyEngines.Contains(preferredEngine))
                    {                        
                        if (IsEngineSuitableForQuality(preferredEngine, quality))
                        {
                            _logger.LogInformation("Selected {Engine} for document type {DocumentType} ", preferredEngine, documentType);
                            return preferredEngine;
                        }
                    }
                }
                var qualityBasedEngine = SelectEngineByQuality(healthyEngines, quality);

                _logger.LogInformation("Selected {Engine} for document type {DocumentType} based on image quality", qualityBasedEngine, documentType);

                return qualityBasedEngine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine optimal engine for document type {DocumentType}", documentType);

                return OCREngine.EasyOCR;
            }
        }

        private bool ShouldReuseEngine(OCREngine engineType)
        {
            try
            {
                if (!_lastHealthChecks.ContainsKey(engineType))
                    return false;

                var lastCheck = _lastHealthChecks[engineType];
                var healthCheckInterval = TimeSpan.FromMinutes(60);

                return DateTime.UtcNow - lastCheck < healthCheckInterval;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside ShouldReuseEngine() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        private List<OCREngine> GetDocumentTypePreferences(string documentType)
        {
            try
            {
                return documentType.ToLowerInvariant() switch
                {
                    "aadhaar" => new List<OCREngine> { OCREngine.EasyOCR, OCREngine.Tesseract },
                    "pan" => new List<OCREngine> { OCREngine.Tesseract, OCREngine.EasyOCR },
                    "passport" => new List<OCREngine> { OCREngine.EasyOCR, OCREngine.Tesseract },
                    "driving_license" or "drivinglicense" => new List<OCREngine> { OCREngine.EasyOCR, OCREngine.Tesseract },
                    "voter_id" or "voterid" => new List<OCREngine> { OCREngine.Tesseract, OCREngine.EasyOCR },
                    _ => new List<OCREngine> { OCREngine.EasyOCR, OCREngine.Tesseract }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetDocumentTypePreferences() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        private bool IsEngineSuitableForQuality(OCREngine engine, ImageQualityMetrics quality)
        {
            try
            {
                return engine switch
                {
                    OCREngine.EasyOCR => quality.OverallQuality >= 0.4f,
                    OCREngine.Tesseract => quality.OverallQuality >= 0.6f,
                    _ => true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside IsEngineSuitableForQuality() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        private OCREngine SelectEngineByQuality(List<OCREngine> availableEngines, ImageQualityMetrics quality)
        {
            try
            {
                if (quality.OverallQuality >= 0.8f && availableEngines.Contains(OCREngine.Tesseract))
                {
                    return OCREngine.Tesseract;
                }

                if (quality.OverallQuality >= 0.5f && availableEngines.Contains(OCREngine.EasyOCR))
                {
                    return OCREngine.EasyOCR;
                }

                if (availableEngines.Contains(OCREngine.EasyOCR))
                {
                    return OCREngine.EasyOCR;
                }

                return availableEngines.First();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside SelectEngineByQuality() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task InitializeAllEnginesAsync()
        {
            try
            {
                _logger.LogInformation("Initializing all available OCR engines");

                var initializationTasks = new List<Task>();

                foreach (var engineType in Enum.GetValues<OCREngine>())
                {
                    if (engineType == OCREngine.Hybrid) continue;

                    initializationTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var engine = CreateEngine(engineType);
                            await engine.InitializeAsync();
                            _logger.LogInformation("{EngineType} engine initialized successfully", engineType);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to initialize {EngineType} engine", engineType);
                        }
                    }));
                }

                await Task.WhenAll(initializationTasks);

                var healthyEngines = await GetHealthyEnginesAsync();
                _logger.LogInformation("OCR engine initialization complete. {HealthyCount} engines are healthy", healthyEngines.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside InitializeAllEnginesAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task DisposeAllEnginesAsync()
        {
            try
            {
                _logger.LogInformation("Disposing all OCR engine instances");

                var disposalTasks = _engineInstances.Values.Select(engine => engine.DisposeAsync());
                await Task.WhenAll(disposalTasks);

                _engineInstances.Clear();
                _lastHealthChecks.Clear();

                _logger.LogInformation("All OCR engines disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside DisposeAllEnginesAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task<Dictionary<OCREngine, OCREngineStatus>> GetAllEngineStatusAsync()
        {
            try
            {
                var statusDict = new Dictionary<OCREngine, OCREngineStatus>();

                foreach (var engineType in Enum.GetValues<OCREngine>())
                {
                    if (engineType == OCREngine.Hybrid) continue;

                    try
                    {
                        var engine = CreateEngine(engineType);
                        var status = await engine.GetStatusAsync();
                        statusDict[engineType] = status;
                    }
                    catch (Exception ex)
                    {
                        statusDict[engineType] = new OCREngineStatus
                        {
                            Engine = engineType,
                            IsAvailable = false,
                            IsHealthy = false,
                            StatusMessage = $"Error: {ex.Message}",
                            LastHealthCheck = DateTime.UtcNow
                        };
                    }
                }
                return statusDict;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetAllEngineStatusAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }

        public async Task<Dictionary<OCREngine, OCREngineCapabilities>> GetAllEngineCapabilitiesAsync()
        {
            try
            {
                var capabilitiesDict = new Dictionary<OCREngine, OCREngineCapabilities>();

                foreach (var engineType in Enum.GetValues<OCREngine>())
                {
                    if (engineType == OCREngine.Hybrid) continue;

                    try
                    {
                        var engine = CreateEngine(engineType);
                        var capabilities = await engine.GetCapabilitiesAsync();
                        capabilitiesDict[engineType] = capabilities;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get capabilities for {EngineType}", engineType);

                        capabilitiesDict[engineType] = new OCREngineCapabilities
                        {
                            Engine = engineType,
                            Version = "Unknown",
                            SupportedLanguages = new List<string>(),
                            SupportedFormats = new List<string>(),
                            AverageAccuracy = 0f,
                            AverageSpeed = 0f
                        };
                    }
                }

                return capabilitiesDict;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetAllEngineCapabilitiesAsync() in OCREngineFactory.cs : " + ex);
                throw;
            }
        }
    }
}
