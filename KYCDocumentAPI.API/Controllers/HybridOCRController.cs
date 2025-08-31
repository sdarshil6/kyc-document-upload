/*
using KYCDocumentAPI.API.Models.Requests;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.ML.OCR.Enums;
using KYCDocumentAPI.ML.OCR.Models;
using KYCDocumentAPI.ML.OCR.Services;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HybridOCRController : ControllerBase
    {
        private readonly IEnhancedOCRService _enhancedOCRService;
        private readonly IOCREngineFactory _engineFactory;
        private readonly ILogger<HybridOCRController> _logger;

        public HybridOCRController(IEnhancedOCRService enhancedOCRService, IOCREngineFactory engineFactory, ILogger<HybridOCRController> logger)
        {
            _enhancedOCRService = enhancedOCRService;
            _engineFactory = engineFactory;
            _logger = logger;
        }

        /// <summary>
        /// Process document with intelligent hybrid OCR (automatic engine selection)
        /// </summary>
        [HttpPost("smart-process")]
        public async Task<ActionResult<ApiResponse<object>>> SmartProcess([FromForm] SmartProcessRequest req)
        {
            try
            {
                if (req.File == null || req.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                var ext = ValidateUploadedFileAndGetExtension(req.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);                
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await req.File.CopyToAsync(stream);
                }

                // Smart processing with automatic engine selection
                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "eng", "hin", "guj" },
                    PreferredEngine = OCREngine.EasyOCR, // Start with EasyOCR for better quality handling
                    EnableFallback = true,
                    PreprocessImage = true,
                    AnalyzeQuality = true,
                    ExtractWordDetails = true,
                    MinimumConfidence = 0.6f,
                    TimeoutSeconds = 10000
                };

                var result = await _enhancedOCRService.ExtractTextAsync(tempPath, options);

                // Cleanup
                System.IO.File.Delete(tempPath);

                var response = new
                {
                    DocumentInfo = new
                    {
                        FileName = req.File.FileName,
                        FileSize = req.File.Length,
                        DocumentType = req.DocumentType,
                        ProcessingMode = "Smart Hybrid"
                    },
                    ProcessingResult = new
                    {
                        Success = result.Success,
                        ExtractedText = result.ExtractedText,
                        OverallConfidence = Math.Round(result.OverallConfidence * 100, 1),
                        ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                        CharacterCount = result.ExtractedText?.Length ?? 0,
                        WordCount = result.TextAnalysis?.TotalWords ?? 0,
                        DetectedLanguages = result.DetectedLanguages
                    },
                    EngineStrategy = new
                    {
                        PrimaryEngine = result.PrimaryEngine.ToString(),
                        FallbackEngine = result.FallbackEngine?.ToString(),
                        UsedFallback = result.ProcessingStats.UsedFallback,
                        EnginesAttempted = result.EngineResults.Count,
                        SelectionReason = DetermineSelectionReason(result)
                    },
                    QualityAssessment = result.QualityMetrics != null ? new
                    {
                        OverallQuality = Math.Round(result.QualityMetrics.OverallQuality * 100, 1),
                        QualityGrade = GetQualityGrade(result.QualityMetrics.OverallQuality),
                        Issues = result.QualityMetrics.QualityIssues,
                        Recommendations = result.QualityMetrics.Recommendations,
                        OptimalForOCR = result.QualityMetrics.OverallQuality >= 0.7f
                    } : null,
                    TextAnalysis = result.TextAnalysis != null ? new
                    {
                        Complexity = result.TextAnalysis.Complexity.ToString(),
                        DetectedPatterns = result.TextAnalysis.DetectedPatterns,
                        ContainsNumbers = result.TextAnalysis.HasNumbers,
                        ContainsDates = result.TextAnalysis.HasDates,
                        LanguageDistribution = result.TextAnalysis.LanguageDistribution
                    } : null,
                    PerformanceMetrics = new
                    {
                        TotalProcessingTime = result.ProcessingTime.TotalMilliseconds,
                        PrimaryEngineTime = result.ProcessingStats.PrimaryEngineTime.TotalMilliseconds,
                        FallbackEngineTime = result.ProcessingStats.FallbackEngineTime.TotalMilliseconds,
                        PreprocessingTime = result.ProcessingStats.ImagePreprocessingTime.TotalMilliseconds,
                        EfficiencyRating = CalculateEfficiencyRating(result)
                    },
                    Recommendation = GenerateRecommendation(result, req.DocumentType)
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Smart hybrid OCR processing completed"));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in TesseractTestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart hybrid OCR processing");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Smart processing failed"));
            }
        }

        /// <summary>
        /// Compare all available OCR engines on the same document
        /// </summary>
        [HttpPost("engine-comparison")]
        public async Task<ActionResult<ApiResponse<object>>> CompareEngines([FromForm] CompareEnginesRequest req)
        {
            try
            {
                if (req.File == null || req.File.Length == 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

                var ext = ValidateUploadedFileAndGetExtension(req.File.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await req.File.CopyToAsync(stream);
                }

                var options = new OCRProcessingOptions
                {
                    Languages = new List<string> { "eng", "hin", "guj" },
                    PreprocessImage = true,
                    ExtractWordDetails = true,
                    MinimumConfidence = 0.5f,
                    TimeoutSeconds = 30
                };

                // Test each engine individually
                var engineResults = new List<object>();
                var availableEngines = await _engineFactory.GetHealthyEnginesAsync();

                foreach (var engineType in availableEngines)
                {
                    try
                    {
                        var engine = _engineFactory.CreateEngine(engineType);
                        var result = await engine.ExtractTextAsync(tempPath, options);

                        engineResults.Add(new
                        {
                            Engine = engineType.ToString(),
                            Success = result.Success,
                            ExtractedText = result.ExtractedText,
                            Confidence = Math.Round(result.Confidence * 100, 1),
                            ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                            CharacterCount = result.ExtractedText?.Length ?? 0,
                            WordCount = result.WordDetails?.Count ?? 0,
                            ErrorMessage = result.ErrorMessage,
                            EngineSpecificData = result.EngineSpecificData?.Take(3).ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString())
                        });
                    }
                    catch (Exception ex)
                    {
                        engineResults.Add(new
                        {
                            Engine = engineType.ToString(),
                            Success = false,
                            ErrorMessage = ex.Message,
                            ProcessingTimeMs = 0
                        });
                    }
                }

                // Cleanup
                System.IO.File.Delete(tempPath);

                // Analyze comparison results
                var successfulResults = engineResults.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!).ToList();
                var bestEngine = successfulResults.OrderByDescending(r =>
                    (double)r.GetType().GetProperty("Confidence")?.GetValue(r)!).FirstOrDefault();

                var response = new
                {
                    FileInfo = new
                    {
                        FileName = req.File.FileName,
                        FileSize = req.File.Length,
                        ProcessingMode = "Engine Comparison"
                    },
                    ComparisonResults = new
                    {
                        TotalEnginesTested = engineResults.Count,
                        SuccessfulEngines = successfulResults.Count,
                        BestEngine = bestEngine?.GetType().GetProperty("Engine")?.GetValue(bestEngine)?.ToString(),
                        BestConfidence = bestEngine != null ?
                            (double)bestEngine.GetType().GetProperty("Confidence")?.GetValue(bestEngine)! : 0,
                        TotalProcessingTime = engineResults.Sum(r =>
                            (double)r.GetType().GetProperty("ProcessingTimeMs")?.GetValue(r)!)
                    },
                    EngineResults = engineResults,
                    Analysis = new
                    {
                        MostAccurate = GetMostAccurateEngine(engineResults),
                        Fastest = GetFastestEngine(engineResults),
                        MostReliable = GetMostReliableEngine(engineResults),
                        Recommendation = GenerateEngineRecommendation(engineResults)
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "Engine comparison completed"));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError("Error occured inside TestTesseractDirect() in TesseractTestController.cs : " + ex);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in engine comparison");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Engine comparison failed"));
            }
        }

        /// <summary>
        /// Get comprehensive system status for all OCR engines
        /// </summary>
        [HttpGet("system-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetSystemStatus()
        {
            try
            {
                var engineStatuses = await _enhancedOCRService.GetEngineStatusAsync();
                var engineCapabilities = await _enhancedOCRService.GetEngineCapabilitiesAsync();
                var performanceMetrics = await _enhancedOCRService.GetPerformanceMetricsAsync();

                var response = new
                {
                    SystemOverview = new
                    {
                        TotalEngines = engineStatuses.Count,
                        HealthyEngines = engineStatuses.Count(s => s.IsHealthy),
                        AvailableEngines = engineStatuses.Count(s => s.IsAvailable),
                        SystemHealth = engineStatuses.Any(s => s.IsHealthy) ? "Operational" : "Critical",
                        LastUpdated = DateTime.UtcNow
                    },
                    EngineStatuses = engineStatuses.Select(s => new
                    {
                        Engine = s.Engine.ToString(),
                        Status = new
                        {
                            IsHealthy = s.IsHealthy,
                            IsAvailable = s.IsAvailable,
                            StatusMessage = s.StatusMessage,
                            LastHealthCheck = s.LastHealthCheck
                        },
                        Performance = new
                        {
                            SuccessfulRequests = s.SuccessfulRequests,
                            FailedRequests = s.FailedRequests,
                            SuccessRate = Math.Round(s.SuccessRate * 100, 1),
                            AverageResponseTimeMs = s.AverageResponseTime.TotalMilliseconds
                        }
                    }),
                    EngineCapabilities = engineCapabilities.Select(c => new
                    {
                        Engine = c.Engine.ToString(),
                        Version = c.Version,
                        Features = new
                        {
                            WordDetails = c.SupportsWordDetails,
                            ConfidenceScores = c.SupportsConfidenceScores,
                            MultipleLanguages = c.SupportsMultipleLanguages,
                            Handwriting = c.SupportsHandwriting
                        },
                        SupportedLanguages = c.SupportedLanguages.Take(10), // Limit for display
                        SupportedFormats = c.SupportedFormats,
                        Performance = new
                        {
                            AverageAccuracy = Math.Round(c.AverageAccuracy * 100, 1),
                            AverageSpeed = c.AverageSpeed
                        }
                    }),
                    SystemMetrics = performanceMetrics,
                    Recommendations = GenerateSystemRecommendations(engineStatuses, engineCapabilities),
                    HybridCapabilities = new
                    {
                        AutomaticEngineSelection = true,
                        FallbackSupport = true,
                        QualityAnalysis = true,
                        MultiLanguageSupport = true,
                        BatchProcessing = true,
                        PerformanceMonitoring = true
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "System status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get system status"));
            }
        }

        /// <summary>
        /// Batch process multiple files with hybrid OCR
        /// </summary>
        [HttpPost("batch-process")]
        public async Task<ActionResult<ApiResponse<object>>> BatchProcess([FromForm] BatchProcessRequest req)
        {
            try
            {
                if (req.Files == null || !req.Files.Any())
                    return BadRequest(ApiResponse<object>.ErrorResponse("No files provided"));

                if (req.Files.Count > 10)
                    return BadRequest(ApiResponse<object>.ErrorResponse("Maximum 10 files allowed per batch"));

                var tempPaths = new List<string>();
                var results = new List<object>();

                try
                {
                    // Save all files temporarily
                    foreach (var file in req.Files)
                    {
                        var tempPath = Path.GetTempFileName();
                        using (var stream = new FileStream(tempPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        tempPaths.Add(tempPath);
                    }

                    // Process batch
                    var options = new OCRProcessingOptions
                    {
                        Languages = new List<string> { "eng", "hin", "guj"},
                        PreferredEngine = OCREngine.EasyOCR,
                        EnableFallback = true,
                        PreprocessImage = true,
                        MinimumConfidence = 0.6f
                    };

                    var batchResults = await _enhancedOCRService.ProcessBatchAsync(tempPaths, options);

                    // Format results
                    for (int i = 0; i < batchResults.Count; i++)
                    {
                        var result = batchResults[i];
                        var file = req.Files[i];

                        results.Add(new
                        {
                            FileIndex = i + 1,
                            FileName = file.FileName,
                            FileSize = file.Length,
                            ProcessingResult = new
                            {
                                Success = result.Success,
                                ExtractedText = result.ExtractedText,
                                Confidence = Math.Round(result.OverallConfidence * 100, 1),
                                ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                                PrimaryEngine = result.PrimaryEngine.ToString(),
                                UsedFallback = result.ProcessingStats.UsedFallback
                            },
                            QualitySummary = result.QualityMetrics != null ? new
                            {
                                Quality = Math.Round(result.QualityMetrics.OverallQuality * 100, 1),
                                Issues = result.QualityMetrics.QualityIssues.Count
                            } : null
                        });
                    }

                    var response = new
                    {
                        BatchSummary = new
                        {
                            TotalFiles = req.Files.Count,
                            SuccessfulProcessing = results.Count(r =>
                                (bool)r.GetType().GetProperty("ProcessingResult")
                                    ?.GetValue(r)?.GetType().GetProperty("Success")?.GetValue(
                                        r.GetType().GetProperty("ProcessingResult")?.GetValue(r))!),
                            TotalProcessingTime = results.Sum(r =>
                                (double)r.GetType().GetProperty("ProcessingResult")
                                    ?.GetValue(r)?.GetType().GetProperty("ProcessingTimeMs")?.GetValue(
                                        r.GetType().GetProperty("ProcessingResult")?.GetValue(r))!),
                            AverageConfidence = results.Average(r =>
                                (double)r.GetType().GetProperty("ProcessingResult")
                                    ?.GetValue(r)?.GetType().GetProperty("Confidence")?.GetValue(
                                        r.GetType().GetProperty("ProcessingResult")?.GetValue(r))!)
                        },
                        Results = results
                    };

                    return Ok(ApiResponse<object>.SuccessResponse(response, "Batch processing completed"));
                }
                finally
                {
                    // Cleanup temp files
                    foreach (var tempPath in tempPaths)
                    {
                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processing");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Batch processing failed"));
            }
        }
        
        private string DetermineSelectionReason(EnhancedOCRResult result)
        {
            try
            {
                if (result.QualityMetrics?.OverallQuality >= 0.8f)
                    return "High quality image - optimal engine selected";
                if (result.QualityMetrics?.OverallQuality < 0.5f)
                    return "Poor quality image - robust engine selected";
                if (result.ProcessingStats.UsedFallback)
                    return "Primary engine failed - fallback engine used";
                return "Standard processing based on document type";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside DetermineSelectionReason() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private string GetQualityGrade(float quality)
        {
            try
            {
                return quality switch
                {
                    >= 0.9f => "Excellent",
                    >= 0.8f => "Good",
                    >= 0.6f => "Fair",
                    >= 0.4f => "Poor",
                    _ => "Very Poor"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetQualityGrade() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private string CalculateEfficiencyRating(EnhancedOCRResult result)
        {
            try
            {
                var totalTime = result.ProcessingTime.TotalMilliseconds;
                var confidence = result.OverallConfidence;

                var efficiency = confidence / (totalTime / 1000); // Confidence per second

                return efficiency switch
                {
                    >= 0.3f => "Excellent",
                    >= 0.2f => "Good",
                    >= 0.1f => "Fair",
                    _ => "Poor"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside CalculateEfficiencyRating() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private string GenerateRecommendation(EnhancedOCRResult result, string documentType)
        {
            try
            {
                if (!result.Success)
                    return "Processing failed - check image quality and try again";

                if (result.OverallConfidence >= 0.9f)
                    return "Excellent results - document ready for processing";

                if (result.OverallConfidence >= 0.7f)
                    return "Good results - minor manual review recommended";

                if (result.QualityMetrics?.QualityIssues.Any() == true)
                    return "Image quality issues detected - consider retaking photo";

                return "Results acceptable but manual verification recommended";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GenerateRecommendation() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private object GetMostAccurateEngine(List<object> results)
        {
            try
            {
                return results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!)
                                 .OrderByDescending(r => (double)r.GetType().GetProperty("Confidence")?.GetValue(r)!)
                                 .FirstOrDefault()?.GetType().GetProperty("Engine")?.GetValue(
                                     results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!)
                                           .OrderByDescending(r => (double)r.GetType().GetProperty("Confidence")?.GetValue(r)!)
                                           .FirstOrDefault()!) ?? "None";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetMostAccurateEngine() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private object GetFastestEngine(List<object> results)
        {
            try
            {
                return results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!)
                                 .OrderBy(r => (double)r.GetType().GetProperty("ProcessingTimeMs")?.GetValue(r)!)
                                 .FirstOrDefault()?.GetType().GetProperty("Engine")?.GetValue(
                                     results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!)
                                           .OrderBy(r => (double)r.GetType().GetProperty("ProcessingTimeMs")?.GetValue(r)!)
                                           .FirstOrDefault()!) ?? "None";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetFastestEngine() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private object GetMostReliableEngine(List<object> results)
        {
            try
            {
                var successfulEngines = results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!);
                return successfulEngines.Any() ? successfulEngines.First().GetType().GetProperty("Engine")?.GetValue(successfulEngines.First()) ?? "None" : "None";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GetMostReliableEngine() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private string GenerateEngineRecommendation(List<object> results)
        {
            try
            {
                var successfulResults = results.Where(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!).ToList();

                if (!successfulResults.Any())
                    return "No engines succeeded - check system configuration";

                if (successfulResults.Count == 1)
                    return $"Only {successfulResults.First().GetType().GetProperty("Engine")?.GetValue(successfulResults.First())} succeeded - consider fixing other engines";

                var bestAccuracy = successfulResults.Max(r => (double)r.GetType().GetProperty("Confidence")?.GetValue(r)!);
                var fastestTime = successfulResults.Min(r => (double)r.GetType().GetProperty("ProcessingTimeMs")?.GetValue(r)!);

                if (bestAccuracy >= 90)
                    return "Multiple engines showing excellent accuracy - hybrid system optimal";
                else
                    return "Multiple engines available - hybrid fallback provides good reliability";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GenerateEngineRecommendation() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private List<string> GenerateSystemRecommendations(List<OCREngineStatus> statuses, List<OCREngineCapabilities> capabilities)
        {
            try
            {
                var recommendations = new List<string>();

                var healthyCount = statuses.Count(s => s.IsHealthy);

                if (healthyCount == 0)
                    recommendations.Add("No OCR engines are healthy - immediate attention required");
                else if (healthyCount == 1)
                    recommendations.Add("Only one engine healthy - consider fixing others for redundancy");
                else
                    recommendations.Add("Multiple healthy engines - excellent system reliability");

                var lowPerformanceEngines = statuses.Where(s => s.SuccessRate < 0.8f && s.SuccessfulRequests > 5).ToList();
                if (lowPerformanceEngines.Any())
                    recommendations.Add($"Low performance detected: {string.Join(", ", lowPerformanceEngines.Select(e => e.Engine))}");

                if (statuses.All(s => s.AverageResponseTime.TotalSeconds < 5))
                    recommendations.Add("All engines performing within acceptable time limits");

                var supportedLanguages = capabilities.SelectMany(c => c.SupportedLanguages).Distinct().Count();
                if (supportedLanguages >= 80)
                    recommendations.Add("Excellent language support across engines");

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside GenerateSystemRecommendations() in HybridOCRController.cs : " + ex);
                throw;
            }
        }

        private string ValidateUploadedFileAndGetExtension(string fileName)
        {
            try
            {
                var ext = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(ext))
                    throw new NotSupportedException("No image extension found. Invalid file uploaded. Kindly upload valid image.");
                else if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("PDF files are not supported. Please upload an image file (PNG,JPG,JPEG,TIFF).");
                return ext;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside ValidateUploadedFileAndGetExtension() in HybridOCRController.cs : " + ex);
                throw;
            }
        }
    }
}
*/