using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.ML.Enums;
using KYCDocumentAPI.ML.Models;
using KYCDocumentAPI.ML.OCR.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KYCDocumentAPI.ML.Services
{
    public class DocumentValidationService : IDocumentValidationService
    {       
        private readonly ILogger<DocumentValidationService> _logger;
        private readonly IOCRService _ocrService;
        private readonly ITextPatternService _textPatternService;

        // Fraud detection thresholds
        private const float HIGH_RISK_THRESHOLD = 0.7f;
        private const float MEDIUM_RISK_THRESHOLD = 0.4f;
        private const float MIN_QUALITY_THRESHOLD = 0.5f;
        private const float MIN_AUTHENTICITY_THRESHOLD = 0.6f;

        public DocumentValidationService(ILogger<DocumentValidationService> logger, IOCRService ocrService, ITextPatternService textPatternService)
        {            
            _logger = logger;
            _ocrService = ocrService;
            _textPatternService = textPatternService;
        }

        public async Task<DocumentValidationResult> ValidateDocumentAsync(Document document)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {                
                if (document == null)                
                    throw new ArgumentException($"Document not found");
                
                var result = await ValidateDocumentFromPathAsync(document.FilePath, document.DocumentType);
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogInformation("Document {DocumentId} validation completed in {ProcessingTime}ms with status {Status}",document.Id, stopwatch.ElapsedMilliseconds, result.Status);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error validating document {DocumentId}", document.Id);

                return new DocumentValidationResult
                {
                    IsValid = false,
                    Status = VerificationStatus.TechnicalError,
                    Summary = $"Validation failed: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<DocumentValidationResult> ValidateDocumentFromPathAsync(string filePath, DocumentType documentType)
        {
            var result = new DocumentValidationResult();
            var checks = new List<ValidationCheck>();

            try
            {
                _logger.LogInformation("Starting validation for {DocumentType} document: {FilePath}", documentType, filePath);

                // Basic file validation
                var fileValidation = await ValidateFileIntegrityAsync(filePath);
                checks.AddRange(fileValidation);

                // Image quality analysis
                var qualityResult = await _ocrService.AnalyzeImageQualityAsync(filePath);
                var qualityChecks = await ValidateImageQualityAsync(qualityResult);
                checks.AddRange(qualityChecks);

                // OCR and text extraction
                var ocrResult = await _ocrService.ExtractTextFromImageAsync(filePath);
                var textChecks = await ValidateTextQualityAsync(ocrResult, documentType);
                checks.AddRange(textChecks);

                // Pattern and structure validation
                if (ocrResult.Success)
                {
                    var patternResult = _textPatternService.AnalyzeText(ocrResult.ExtractedText, Path.GetFileName(filePath));
                    var structureChecks = await ValidateDocumentStructureAsync(patternResult, documentType);
                    checks.AddRange(structureChecks);

                    // Authenticity checks
                    var authenticityScore = await CalculateAuthenticityScoreAsync(ocrResult.ExtractedText, documentType);
                    var authenticityChecks = await ValidateAuthenticityAsync(authenticityScore, patternResult);
                    checks.AddRange(authenticityChecks);
                }

                // Fraud detection analysis
                var fraudInput = await CreateFraudDetectionInputAsync(filePath, documentType, ocrResult, qualityResult);
                var fraudChecks = await RunFraudDetectionAsync(fraudInput);
                checks.AddRange(fraudChecks);

                // Security and tampering detection
                var securityChecks = await RunSecurityChecksAsync(filePath, documentType);
                checks.AddRange(securityChecks);

                // Calculate overall metrics and status
                result.Checks = checks;
                result.Metrics = await CalculateVerificationMetricsAsync(fraudInput);
                result.Status = DetermineVerificationStatus(result.Metrics, checks);
                result.IsValid = result.Status == VerificationStatus.Authentic;
                result.Summary = GenerateValidationSummary(result.Metrics, checks);

                _logger.LogInformation("Validation completed with status {Status} and overall score {Score}",result.Status, result.Metrics.OverallScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in document validation for {FilePath}", filePath);

                return new DocumentValidationResult
                {
                    IsValid = false,
                    Status = VerificationStatus.TechnicalError,
                    Summary = $"Validation error: {ex.Message}",
                    Checks = checks
                };
            }
        }

        public async Task<VerificationMetrics> CalculateVerificationMetricsAsync(FraudDetectionInput input)
        {
            try
            {
                await Task.CompletedTask;

                var metrics = new VerificationMetrics();

                // Calculate Quality Score (0-1)
                metrics.QualityScore = (
                    input.ImageBrightness * 0.2f +
                    input.ImageContrast * 0.2f +
                    input.ImageSharpness * 0.3f +
                    (1 - input.NoiseLevel) * 0.2f +
                    input.OCRConfidence * 0.1f
                );

                // Calculate Authenticity Score (0-1)
                metrics.AuthenticityScore = (
                    (input.HasValidNumberFormat ? 0.3f : 0f) +
                    (input.HasConsistentDateFormats ? 0.2f : 0f) +
                    (input.HasExpectedDocumentStructure ? 0.3f : 0f) +
                    input.LanguageConsistency * 0.2f
                );

                // Calculate Consistency Score (0-1)
                metrics.ConsistencyScore = (
                    input.NameConsistency * 0.4f +
                    input.DateConsistency * 0.3f +
                    input.AddressConsistency * 0.3f
                );

                // Calculate Fraud Risk Score (0-1, lower is better)
                var tamperingIndicators = (
                    (input.HasUnexpectedFonts ? 0.2f : 0f) +
                    (input.HasInconsistentSpacing ? 0.15f : 0f) +
                    (input.HasColorAnomalies ? 0.25f : 0f) +
                    (input.HasCompressionArtifacts ? 0.1f : 0f)
                );

                var statisticalAnomalies = (
                    (1 - input.NumberDistribution) * 0.1f +
                    (1 - input.TextDistribution) * 0.1f +
                    (1 - input.ColorDistribution) * 0.1f
                );

                metrics.FraudRiskScore = tamperingIndicators + statisticalAnomalies;

                // Calculate Overall Score
                metrics.OverallScore = (
                    metrics.QualityScore * 0.25f +
                    metrics.AuthenticityScore * 0.35f +
                    metrics.ConsistencyScore * 0.25f +
                    (1 - metrics.FraudRiskScore) * 0.15f
                );

                // Populate detailed scores
                metrics.DetailedScores = new Dictionary<string, float>
            {
                { "Image_Quality", metrics.QualityScore },
                { "OCR_Confidence", input.OCRConfidence },
                { "Text_Quality", input.TextQuality },
                { "Structure_Validation", input.HasExpectedDocumentStructure ? 1.0f : 0.0f },
                { "Number_Format_Validation", input.HasValidNumberFormat ? 1.0f : 0.0f },
                { "Date_Format_Consistency", input.HasConsistentDateFormats ? 1.0f : 0.0f },
                { "Tampering_Risk", tamperingIndicators },
                { "Statistical_Anomalies", statisticalAnomalies }
            };

                // Generate insights
                GenerateValidationInsights(metrics, input);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateVerificationMetricsAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        public async Task<List<ValidationCheck>> RunSecurityChecksAsync(string filePath, DocumentType documentType)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                // File integrity checks
                checks.Add(new ValidationCheck
                {
                    CheckName = "File_Integrity",
                    Category = "Security",
                    Passed = await ValidateFileFormatAsync(filePath),
                    Score = 1.0f,
                    Description = "Validates file format and structure",
                    Severity = CheckSeverity.High
                });

                // Metadata analysis
                checks.Add(new ValidationCheck
                {
                    CheckName = "Metadata_Analysis",
                    Category = "Security",
                    Passed = await ValidateFileMetadataAsync(filePath),
                    Score = 0.9f,
                    Description = "Analyzes file metadata for tampering signs",
                    Severity = CheckSeverity.Medium
                });

                // Digital signature check (for PDFs)
                if (Path.GetExtension(filePath).ToLower() == ".pdf")
                {
                    checks.Add(new ValidationCheck
                    {
                        CheckName = "Digital_Signature",
                        Category = "Security",
                        Passed = await ValidateDigitalSignatureAsync(filePath),
                        Score = 0.8f,
                        Description = "Checks for digital signatures in PDF documents",
                        Severity = CheckSeverity.Medium
                    });
                }

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside RunSecurityChecksAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        public async Task<bool> DetectTamperingAsync(string filePath)
        {
            try
            {
                await Task.CompletedTask;

                // Mock tampering detection based on file analysis
                var fileInfo = new FileInfo(filePath);
                var fileName = fileInfo.Name.ToLower();

                // Simulate tampering detection logic
                var suspiciousIndicators = 0;

                // Check file size anomalies
                if (fileInfo.Length < 10000 || fileInfo.Length > 5000000) // Too small or too large
                    suspiciousIndicators++;

                // Check for suspicious filename patterns
                if (fileName.Contains("edited") || fileName.Contains("modified") || fileName.Contains("fake"))
                    suspiciousIndicators++;

                // Simulate image analysis results
                var random = new Random();
                if (random.NextDouble() < 0.1) // 10% chance of detecting tampering
                    suspiciousIndicators += 2;

                return suspiciousIndicators >= 2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tampering detection for {FilePath}", filePath);
                return false;
            }
        }

        public async Task<float> CalculateAuthenticityScoreAsync(string extractedText, DocumentType documentType)
        {
            try
            {
                await Task.CompletedTask;

                var score = 0.0f;
                var maxScore = 0.0f;

                // Document-specific authenticity checks
                switch (documentType)
                {
                    case DocumentType.Aadhaar:
                        score += CheckAadhaarAuthenticity(extractedText);
                        maxScore = 10.0f;
                        break;

                    case DocumentType.PAN:
                        score += CheckPANAuthenticity(extractedText);
                        maxScore = 8.0f;
                        break;

                    case DocumentType.Passport:
                        score += CheckPassportAuthenticity(extractedText);
                        maxScore = 12.0f;
                        break;

                    default:
                        score += CheckGenericAuthenticity(extractedText);
                        maxScore = 6.0f;
                        break;
                }

                return Math.Min(score / maxScore, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateAuthenticityScoreAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
        
        private void GenerateValidationInsights(VerificationMetrics metrics, FraudDetectionInput input)
        {
            try
            {
                // Risk factors
                if (metrics.QualityScore < MIN_QUALITY_THRESHOLD)
                    metrics.RiskFactors.Add("Poor image quality detected");

                if (metrics.AuthenticityScore < MIN_AUTHENTICITY_THRESHOLD)
                    metrics.RiskFactors.Add("Document authenticity concerns");

                if (input.HasColorAnomalies)
                    metrics.RiskFactors.Add("Unusual color patterns detected");

                if (input.HasUnexpectedFonts)
                    metrics.RiskFactors.Add("Inconsistent font usage");

                if (metrics.FraudRiskScore > HIGH_RISK_THRESHOLD)
                    metrics.RiskFactors.Add("High fraud risk indicators");

                // Quality issues
                if (input.ImageSharpness < 0.6f)
                    metrics.QualityIssues.Add("Image appears blurry");

                if (input.ImageBrightness < 0.3f)
                    metrics.QualityIssues.Add("Image is too dark");

                if (input.NoiseLevel > 0.3f)
                    metrics.QualityIssues.Add("High noise level in image");

                if (input.OCRConfidence < 0.7f)
                    metrics.QualityIssues.Add("Low OCR confidence");

                // Positive indicators
                if (metrics.QualityScore > 0.8f)
                    metrics.PositiveIndicators.Add("Excellent image quality");

                if (metrics.AuthenticityScore > 0.9f)
                    metrics.PositiveIndicators.Add("Strong authenticity indicators");

                if (input.HasValidNumberFormat && input.HasConsistentDateFormats)
                    metrics.PositiveIndicators.Add("Valid document format structure");

                if (metrics.ConsistencyScore > 0.8f)
                    metrics.PositiveIndicators.Add("High data consistency");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GenerateValidationInsights() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<FraudDetectionInput> CreateFraudDetectionInputAsync(string filePath, DocumentType documentType, EnhancedOCRResult ocrResult, ImageQualityMetrics qualityResult)
        {
            try
            {
                await Task.CompletedTask;

                var fileInfo = new FileInfo(filePath);
                var patternResult = _textPatternService.AnalyzeText(ocrResult.ExtractedText, fileInfo.Name);

                return new FraudDetectionInput
                {
                    DocumentType = documentType.ToString(),
                    FileSize = fileInfo.Length,
                    FileExtension = fileInfo.Extension.ToLowerInvariant(),

                    OCRConfidence = ocrResult.OverallConfidence,
                    TextQuality = ocrResult.Success ? 0.8f : 0.3f,
                    TextLength = ocrResult.ExtractedText.Length,
                    LanguageConsistency = CalculateLanguageConsistency(ocrResult.ExtractedText),

                    ImageBrightness = qualityResult.Brightness,
                    ImageContrast = qualityResult.Contrast,
                    ImageSharpness = qualityResult.Sharpness,
                    NoiseLevel = qualityResult.NoiseLevel,

                    HasValidNumberFormat = ValidateNumberFormats(patternResult, documentType),
                    HasConsistentDateFormats = ValidateDateFormats(ocrResult.ExtractedText),
                    HasExpectedDocumentStructure = ValidateDocumentStructure(ocrResult.ExtractedText, documentType),

                    NameConsistency = CalculateNameConsistency(ocrResult.ExtractedText),
                    DateConsistency = CalculateDateConsistency(ocrResult.ExtractedText),
                    AddressConsistency = CalculateAddressConsistency(ocrResult.ExtractedText),

                    NumberDistribution = AnalyzeNumberDistribution(ocrResult.ExtractedText),
                    TextDistribution = AnalyzeTextDistribution(ocrResult.ExtractedText),
                    ColorDistribution = 0.8f, // Would be calculated from actual image analysis

                    HasUnexpectedFonts = DetectFontAnomalies(filePath),
                    HasInconsistentSpacing = DetectSpacingAnomalies(ocrResult.ExtractedText),
                    HasColorAnomalies = qualityResult.QualityIssues.Any(q => q.Contains("color")),
                    HasCompressionArtifacts = DetectCompressionArtifacts(filePath),

                    UserHistoryScore = 0.8f, // Would be calculated from user's document history
                    DocumentHistoryScore = 0.9f // Would be calculated from similar documents
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CreateFraudDetectionInputAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
        
        private float CheckAadhaarAuthenticity(string text)
        {
            try
            {
                float score = 0f;

                if (text.Contains("government of india", StringComparison.OrdinalIgnoreCase)) score += 2f;
                if (text.Contains("unique identification", StringComparison.OrdinalIgnoreCase)) score += 2f;
                if (text.Contains("आधार", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (Regex.IsMatch(text, @"\b\d{4}\s?\d{4}\s?\d{4}\b")) score += 3f; // Aadhaar pattern
                if (text.Contains("male", StringComparison.OrdinalIgnoreCase) || text.Contains("female", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (Regex.IsMatch(text, @"\b\d{6}\b")) score += 1f; // PIN code

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckAadhaarAuthenticity() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CheckPANAuthenticity(string text)
        {
            try
            {
                float score = 0f;

                if (text.Contains("income tax department", StringComparison.OrdinalIgnoreCase)) score += 2f;
                if (text.Contains("permanent account number", StringComparison.OrdinalIgnoreCase)) score += 2f;
                if (text.Contains("govt of india", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (Regex.IsMatch(text, @"\b[A-Z]{5}\d{4}[A-Z]\b")) score += 2f; // PAN pattern
                if (text.Contains("signature", StringComparison.OrdinalIgnoreCase)) score += 1f;

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckPANAuthenticity() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CheckPassportAuthenticity(string text)
        {
            try
            {
                float score = 0f;

                if (text.Contains("republic of india", StringComparison.OrdinalIgnoreCase)) score += 3f;
                if (text.Contains("passport", StringComparison.OrdinalIgnoreCase)) score += 2f;
                if (text.Contains("nationality", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (text.Contains("indian", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (Regex.IsMatch(text, @"\b[A-Z]\d{7}\b")) score += 2f; // Passport number pattern
                if (text.Contains("date of birth", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (text.Contains("place of birth", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (text.Contains("date of expiry", StringComparison.OrdinalIgnoreCase)) score += 1f;

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckPassportAuthenticity() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CheckGenericAuthenticity(string text)
        {
            try
            {
                float score = 0f;

                if (text.Contains("government", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (text.Contains("india", StringComparison.OrdinalIgnoreCase)) score += 1f;
                if (Regex.IsMatch(text, @"\b\d{1,2}[/\-]\d{1,2}[/\-]\d{4}\b")) score += 1f; // Date pattern
                if (text.Length > 50) score += 1f; // Sufficient content
                if (Regex.IsMatch(text, @"[A-Za-z]{3,}")) score += 1f; // Contains words
                if (Regex.IsMatch(text, @"\d+")) score += 1f; // Contains numbers

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckGenericAuthenticity() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
        
        private async Task<List<ValidationCheck>> ValidateFileIntegrityAsync(string filePath)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                var fileExists = File.Exists(filePath);
                checks.Add(new ValidationCheck
                {
                    CheckName = "File_Exists",
                    Category = "File_Integrity",
                    Passed = fileExists,
                    Score = fileExists ? 1.0f : 0.0f,
                    Description = "Verifies that the file exists and is accessible",
                    Severity = CheckSeverity.Critical
                });

                if (fileExists)
                {
                    var fileInfo = new FileInfo(filePath);
                    var validSize = fileInfo.Length > 0 && fileInfo.Length < 10 * 1024 * 1024; // 0-10MB

                    checks.Add(new ValidationCheck
                    {
                        CheckName = "File_Size",
                        Category = "File_Integrity",
                        Passed = validSize,
                        Score = validSize ? 1.0f : 0.5f,
                        Description = $"File size: {fileInfo.Length / 1024}KB",
                        Details = validSize ? "File size is within acceptable range" : "File size is outside normal range",
                        Severity = validSize ? CheckSeverity.Info : CheckSeverity.Medium
                    });
                }

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateFileIntegrityAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<List<ValidationCheck>> ValidateImageQualityAsync(ImageQualityMetrics qualityResult)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                checks.Add(new ValidationCheck
                {
                    CheckName = "Overall_Quality",
                    Category = "Image_Quality",
                    Passed = qualityResult.OverallQuality >= MIN_QUALITY_THRESHOLD,
                    Score = qualityResult.OverallQuality,
                    Description = $"Overall image quality: {Math.Round(qualityResult.OverallQuality * 100, 1)}%",
                    Details = string.Join(", ", qualityResult.QualityIssues),
                    Severity = qualityResult.OverallQuality >= 0.7f ? CheckSeverity.Info :
                              qualityResult.OverallQuality >= 0.5f ? CheckSeverity.Medium : CheckSeverity.High
                });

                checks.Add(new ValidationCheck
                {
                    CheckName = "Image_Sharpness",
                    Category = "Image_Quality",
                    Passed = !qualityResult.IsBlurry,
                    Score = qualityResult.Sharpness,
                    Description = $"Image sharpness: {Math.Round(qualityResult.Sharpness * 100, 1)}%",
                    Details = qualityResult.IsBlurry ? "Image appears blurry" : "Image sharpness is acceptable",
                    Severity = qualityResult.IsBlurry ? CheckSeverity.High : CheckSeverity.Info
                });

                checks.Add(new ValidationCheck
                {
                    CheckName = "Image_Brightness",
                    Category = "Image_Quality",
                    Passed = !qualityResult.IsTooDark && !qualityResult.IsTooLight,
                    Score = Math.Min(qualityResult.Brightness, 1 - qualityResult.Brightness) * 2, // Optimal around 0.5
                    Description = $"Image brightness: {Math.Round(qualityResult.Brightness * 100, 1)}%",
                    Details = qualityResult.IsTooDark ? "Image is too dark" :
                             qualityResult.IsTooLight ? "Image is overexposed" : "Brightness is optimal",
                    Severity = (qualityResult.IsTooDark || qualityResult.IsTooLight) ? CheckSeverity.Medium : CheckSeverity.Info
                });

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateImageQualityAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<List<ValidationCheck>> ValidateTextQualityAsync(EnhancedOCRResult ocrResult, DocumentType documentType)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                checks.Add(new ValidationCheck
                {
                    CheckName = "OCR_Success",
                    Category = "Text_Quality",
                    Passed = ocrResult.Success,
                    Score = ocrResult.Success ? 1.0f : 0.0f,
                    Description = "OCR text extraction status",
                    Details = ocrResult.Success ? "Text extracted successfully" : $"OCR failed: {string.Join(", ", ocrResult.Errors)}",
                    Severity = ocrResult.Success ? CheckSeverity.Info : CheckSeverity.Critical
                });

                if (ocrResult.Success)
                {
                    checks.Add(new ValidationCheck
                    {
                        CheckName = "OCR_Confidence",
                        Category = "Text_Quality",
                        Passed = ocrResult.OverallConfidence >= 0.7f,
                        Score = ocrResult.OverallConfidence,
                        Description = $"OCR confidence: {Math.Round(ocrResult.OverallConfidence * 100, 1)}%",
                        Details = ocrResult.OverallConfidence >= 0.8f ? "High confidence" :
                                 ocrResult.OverallConfidence >= 0.6f ? "Medium confidence" : "Low confidence",
                        Severity = ocrResult.OverallConfidence >= 0.7f ? CheckSeverity.Info : CheckSeverity.Medium
                    });

                    var textLength = ocrResult.ExtractedText.Length;
                    var hasMinimumContent = textLength >= 50; // Minimum expected content

                    checks.Add(new ValidationCheck
                    {
                        CheckName = "Text_Content_Length",
                        Category = "Text_Quality",
                        Passed = hasMinimumContent,
                        Score = Math.Min(textLength / 200.0f, 1.0f), // Normalize to 200 chars
                        Description = $"Extracted text length: {textLength} characters",
                        Details = hasMinimumContent ? "Sufficient text content" : "Insufficient text content extracted",
                        Severity = hasMinimumContent ? CheckSeverity.Info : CheckSeverity.High
                    });
                }

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateTextQualityAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<List<ValidationCheck>> ValidateDocumentStructureAsync(DocumentPatternResult patternResult, DocumentType documentType)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                // Document type consistency check
                var typeConsistent = patternResult.PredictedDocumentType == documentType || patternResult.Confidence < 0.8;
                checks.Add(new ValidationCheck
                {
                    CheckName = "Document_Type_Consistency",
                    Category = "Structure",
                    Passed = typeConsistent,
                    Score = typeConsistent ? 1.0f : 0.3f,
                    Description = $"Expected: {documentType}, Detected: {patternResult.PredictedDocumentType}",
                    Details = typeConsistent ? "Document type matches expectation" :
                             $"Document type mismatch (confidence: {Math.Round(patternResult.Confidence * 100, 1)}%)",
                    Severity = typeConsistent ? CheckSeverity.Info : CheckSeverity.High
                });

                // Pattern validation based on document type
                switch (documentType)
                {
                    case DocumentType.Aadhaar:
                        checks.Add(new ValidationCheck
                        {
                            CheckName = "Aadhaar_Number_Pattern",
                            Category = "Structure",
                            Passed = patternResult.HasAadhaarPattern,
                            Score = patternResult.HasAadhaarPattern ? 1.0f : 0.0f,
                            Description = "Aadhaar number pattern validation",
                            Details = patternResult.HasAadhaarPattern ?
                                    $"Valid Aadhaar number found: {patternResult.AadhaarNumber}" :
                                    "No valid Aadhaar number pattern detected",
                            Severity = patternResult.HasAadhaarPattern ? CheckSeverity.Info : CheckSeverity.High
                        });
                        break;

                    case DocumentType.PAN:
                        checks.Add(new ValidationCheck
                        {
                            CheckName = "PAN_Number_Pattern",
                            Category = "Structure",
                            Passed = patternResult.HasPANPattern,
                            Score = patternResult.HasPANPattern ? 1.0f : 0.0f,
                            Description = "PAN number pattern validation",
                            Details = patternResult.HasPANPattern ?
                                    $"Valid PAN number found: {patternResult.PANNumber}" :
                                    "No valid PAN number pattern detected",
                            Severity = patternResult.HasPANPattern ? CheckSeverity.Info : CheckSeverity.High
                        });
                        break;

                    case DocumentType.Passport:
                        checks.Add(new ValidationCheck
                        {
                            CheckName = "Passport_Number_Pattern",
                            Category = "Structure",
                            Passed = patternResult.HasPassportPattern,
                            Score = patternResult.HasPassportPattern ? 1.0f : 0.0f,
                            Description = "Passport number pattern validation",
                            Details = patternResult.HasPassportPattern ?
                                    $"Valid passport number found: {patternResult.PassportNumber}" :
                                    "No valid passport number pattern detected",
                            Severity = patternResult.HasPassportPattern ? CheckSeverity.Info : CheckSeverity.Medium
                        });
                        break;
                }

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateDocumentStructureAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<bool> ValidateFileMetadataAsync(string filePath)
        {
            try
            {
                await Task.CompletedTask;

                var fileInfo = new FileInfo(filePath);

                // Check file creation and modification times for anomalies
                var creationTime = fileInfo.CreationTime;
                var lastWriteTime = fileInfo.LastWriteTime;
                var timeDifference = Math.Abs((lastWriteTime - creationTime).TotalMinutes);

                // Suspicious if modified significantly after creation (potential tampering)
                if (timeDifference > 60) // More than 1 hour difference
                {
                    _logger.LogWarning("File {FilePath} has suspicious timestamp difference: {TimeDiff} minutes",
                        filePath, Math.Round(timeDifference, 1));
                    return false;
                }

                // Check file extension consistency
                var extension = fileInfo.Extension.ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("File {FilePath} has unsupported extension: {Extension}", filePath, extension);
                    return false;
                }

                // Additional metadata checks for images
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
                {
                    // Mock EXIF data validation
                    var hasValidImageMetadata = ValidateImageMetadata(filePath);
                    return hasValidImageMetadata;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file metadata for {FilePath}", filePath);
                return false;
            }
        }

        private async Task<bool> ValidateFileFormatAsync(string filePath)
        {
            try
            {
                await Task.CompletedTask;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

                if (!allowedExtensions.Contains(extension))
                    return false;

                // Read file header to validate actual format matches extension
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var header = new byte[10];
                await fileStream.ReadAsync(header, 0, 10);

                return extension switch
                {
                    ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8, // JPEG magic bytes
                    ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47, // PNG magic bytes
                    ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // PDF magic bytes
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file format for {FilePath}", filePath);
                return false;
            }
        }

        private async Task<bool> ValidateDigitalSignatureAsync(string filePath)
        {
            try
            {
                await Task.CompletedTask;

                if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return true; // Not applicable for non-PDF files

                // Mock digital signature validation
                // In production, use a PDF library like iTextSharp or PdfSharp
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[1024];
                await fileStream.ReadAsync(buffer, 0, 1024);

                var content = System.Text.Encoding.UTF8.GetString(buffer);

                // Look for digital signature indicators in PDF content
                var hasSignatureIndicators = content.Contains("/Sig") ||
                                            content.Contains("/ByteRange") ||
                                            content.Contains("/Contents");

                // For demo purposes, consider signature valid if indicators are present
                return hasSignatureIndicators;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating digital signature for {FilePath}", filePath);
                return false;
            }
        }

        // Text consistency analysis methods
        private float CalculateNameConsistency(string text)
        {
            try
            {
                var names = ExtractAllNames(text);
                if (names.Count <= 1)
                    return 1.0f; // Single name or no names found

                // Calculate similarity between names
                var consistencyScore = 0.0f;
                var comparisons = 0;

                for (int i = 0; i < names.Count - 1; i++)
                {
                    for (int j = i + 1; j < names.Count; j++)
                    {
                        var similarity = CalculateStringSimilarity(names[i], names[j]);
                        consistencyScore += similarity;
                        comparisons++;
                    }
                }

                return comparisons > 0 ? consistencyScore / comparisons : 1.0f;
            }
            catch
            {
                return 0.5f; // Default moderate consistency
            }
        }

        private float CalculateDateConsistency(string text)
        {
            try
            {
                var dates = ExtractAllDates(text);
                if (dates.Count <= 1)
                    return 1.0f;

                // Check for reasonable date ranges and formats
                var consistencyScore = 1.0f;
                var currentYear = DateTime.Now.Year;

                foreach (var date in dates)
                {
                    // Check if dates are within reasonable ranges
                    if (date.Year < 1900 || date.Year > currentYear + 1)
                        consistencyScore -= 0.3f;

                    // Check for birth dates that are too recent or too old
                    var age = currentYear - date.Year;
                    if (age < 0 || age > 120)
                        consistencyScore -= 0.2f;
                }

                return Math.Max(consistencyScore, 0.0f);
            }
            catch
            {
                return 0.7f; // Default good consistency
            }
        }

        private float CalculateAddressConsistency(string text)
        {
            try
            {
                var addresses = ExtractAllAddresses(text);
                if (addresses.Count <= 1)
                    return 1.0f;

                // Check for consistent state/city/PIN code patterns
                var consistencyScore = 1.0f;
                var pinCodes = new HashSet<string>();
                var states = new HashSet<string>();

                foreach (var address in addresses)
                {
                    var pinCode = ExtractPinCode(address);
                    var state = ExtractStateFromAddress(address);

                    if (!string.IsNullOrEmpty(pinCode))
                        pinCodes.Add(pinCode);
                    if (!string.IsNullOrEmpty(state))
                        states.Add(state);
                }

                // Penalize if multiple different PIN codes or states
                if (pinCodes.Count > 1)
                    consistencyScore -= 0.4f;
                if (states.Count > 1)
                    consistencyScore -= 0.3f;

                return Math.Max(consistencyScore, 0.0f);
            }
            catch
            {
                return 0.8f; // Default good consistency
            }
        }

        // Format validation methods
        private bool ValidateNumberFormats(DocumentPatternResult patternResult, DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Aadhaar => patternResult.HasAadhaarPattern &&
                                       !string.IsNullOrEmpty(patternResult.AadhaarNumber) &&
                                       patternResult.AadhaarNumber.Length == 12 &&
                                       patternResult.AadhaarNumber.All(char.IsDigit),

                DocumentType.PAN => patternResult.HasPANPattern &&
                                   !string.IsNullOrEmpty(patternResult.PANNumber) &&
                                   Regex.IsMatch(patternResult.PANNumber, @"^[A-Z]{5}\d{4}[A-Z]$"),

                DocumentType.Passport => patternResult.HasPassportPattern &&
                                        !string.IsNullOrEmpty(patternResult.PassportNumber) &&
                                        Regex.IsMatch(patternResult.PassportNumber, @"^[A-Z]\d{7}$"),

                _ => true // Generic documents don't have specific number format requirements
            };
        }

        private float CalculateLanguageConsistency(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return 0.5f;

                var englishChars = 0;
                var hindiChars = 0;
                var digitChars = 0;
                var otherChars = 0;

                foreach (char c in text)
                {
                    if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                        continue;

                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                        englishChars++;
                    else if (c >= '\u0900' && c <= '\u097F') // Devanagari Unicode range
                        hindiChars++;
                    else if (char.IsDigit(c))
                        digitChars++;
                    else
                        otherChars++;
                }

                var totalChars = englishChars + hindiChars + digitChars + otherChars;
                if (totalChars == 0)
                    return 0.5f;

                // Calculate consistency based on dominant language
                var englishRatio = (float)englishChars / totalChars;
                var hindiRatio = (float)hindiChars / totalChars;
                var digitRatio = (float)digitChars / totalChars;

                // Good consistency if one language dominates or reasonable mix
                var dominantLanguage = Math.Max(englishRatio, hindiRatio);
                var consistency = dominantLanguage + (digitRatio * 0.5f); // Numbers are neutral

                return Math.Min(consistency, 1.0f);
            }
            catch
            {
                return 0.7f; // Default moderate consistency
            }
        }

        private bool ValidateDateFormats(string text)
        {
            var datePatterns = new[]
            {
        @"\b\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{4}\b", // DD/MM/YYYY or DD-MM-YYYY
        @"\b\d{4}[\/\-\.]\d{1,2}[\/\-\.]\d{1,2}\b", // YYYY/MM/DD
        @"\b\d{1,2}\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{4}\b", // DD MON YYYY
        @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{1,2},?\s+\d{4}\b" // MON DD, YYYY
    };

            var foundDates = 0;
            var validDates = 0;

            foreach (var pattern in datePatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foundDates += matches.Count;

                foreach (Match match in matches)
                {
                    if (IsValidDateString(match.Value))
                        validDates++;
                }
            }

            return foundDates == 0 || (float)validDates / foundDates >= 0.8f; // 80% of dates should be valid
        }

        private bool ValidateDocumentStructure(string text, DocumentType documentType)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 50)
                return false;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3)
                return false;

            return documentType switch
            {
                DocumentType.Aadhaar => ValidateAadhaarStructure(text),
                DocumentType.PAN => ValidatePANStructure(text),
                DocumentType.Passport => ValidatePassportStructure(text),
                DocumentType.DrivingLicense => ValidateDrivingLicenseStructure(text),
                _ => ValidateGenericStructure(text)
            };
        }

        // Statistical analysis methods
        private float AnalyzeNumberDistribution(string text)
        {
            try
            {
                var numbers = Regex.Matches(text, @"\d+").Cast<Match>().Select(m => m.Value).ToList();
                if (numbers.Count == 0)
                    return 0.8f; // Neutral if no numbers

                // Analyze distribution of digits
                var digitCounts = new int[10];
                foreach (var number in numbers)
                {
                    foreach (char digit in number)
                    {
                        if (char.IsDigit(digit))
                            digitCounts[digit - '0']++;
                    }
                }

                var totalDigits = digitCounts.Sum();
                if (totalDigits == 0)
                    return 0.8f;

                // Calculate entropy - normal distribution should have balanced digit usage
                var entropy = 0.0;
                for (int i = 0; i < 10; i++)
                {
                    if (digitCounts[i] > 0)
                    {
                        var probability = (double)digitCounts[i] / totalDigits;
                        entropy -= probability * Math.Log2(probability);
                    }
                }

                // Normalize entropy (max entropy for uniform distribution is log2(10) ≈ 3.32)
                var normalizedEntropy = entropy / 3.32;

                return (float)Math.Min(normalizedEntropy, 1.0);
            }
            catch
            {
                return 0.8f; // Default normal distribution
            }
        }

        private float AnalyzeTextDistribution(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return 0.5f;

                var letterCounts = new int[26];
                var totalLetters = 0;

                foreach (char c in text.ToUpperInvariant())
                {
                    if (c >= 'A' && c <= 'Z')
                    {
                        letterCounts[c - 'A']++;
                        totalLetters++;
                    }
                }

                if (totalLetters == 0)
                    return 0.7f;

                // Calculate letter frequency distribution
                // English has known frequency patterns - check if text follows natural patterns
                var expectedFrequencies = new[]
                {
            8.12, 1.49, 2.78, 4.25, 12.02, 2.23, 2.02, 6.09, 6.97, 0.15, 0.77, 4.03,
            2.41, 6.75, 7.51, 1.93, 0.10, 5.99, 6.33, 9.06, 2.76, 0.98, 2.36, 0.15, 1.97, 0.07
        };

                var actualFrequencies = letterCounts.Select(count => (double)count / totalLetters * 100).ToArray();

                // Calculate similarity to expected English frequency distribution
                var similarity = 1.0 - CalculateFrequencyDifference(expectedFrequencies, actualFrequencies) / 100.0;

                return (float)Math.Max(0.3, Math.Min(similarity, 1.0));
            }
            catch
            {
                return 0.7f; // Default normal distribution
            }
        }

        // Tampering detection methods
        private bool DetectFontAnomalies(string filePath)
        {
            try
            {
                // Mock font analysis - in production, use image processing libraries
                var fileName = Path.GetFileName(filePath).ToLower();

                // Simulate font inconsistency detection based on filename patterns
                if (fileName.Contains("edited") || fileName.Contains("modified"))
                    return true;

                // Random chance for demonstration (2% chance of detecting font anomalies)
                var random = new Random();
                return random.NextDouble() < 0.02;
            }
            catch
            {
                return false;
            }
        }

        private bool DetectSpacingAnomalies(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                // Detect unusual spacing patterns
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var spacingAnomalies = 0;

                foreach (var line in lines)
                {
                    // Check for excessive spaces
                    if (Regex.IsMatch(line, @"\s{5,}")) // 5 or more consecutive spaces
                        spacingAnomalies++;

                    // Check for inconsistent spacing around punctuation
                    if (Regex.IsMatch(line, @"\w\s{2,}[.,;:]") || Regex.IsMatch(line, @"[.,;:]\s{3,}\w"))
                        spacingAnomalies++;
                }

                // Consider anomalous if more than 20% of lines have spacing issues
                return lines.Length > 0 && (float)spacingAnomalies / lines.Length > 0.2f;
            }
            catch
            {
                return false;
            }
        }

        private bool DetectCompressionArtifacts(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var extension = fileInfo.Extension.ToLowerInvariant();

                if (extension != ".jpg" && extension != ".jpeg")
                    return false; // Only applicable to JPEG files

                // Mock compression artifact detection
                // In production, analyze JPEG quality factors and compression ratios

                // Very small file size might indicate high compression
                var sizeInKB = fileInfo.Length / 1024;
                if (sizeInKB < 50) // Less than 50KB might be over-compressed
                    return true;

                // Very large file size might indicate multiple compression cycles
                if (sizeInKB > 2048) // More than 2MB might be suspicious
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Status determination methods
        private VerificationStatus DetermineVerificationStatus(VerificationMetrics metrics, List<ValidationCheck> checks)
        {
            var criticalFailures = checks.Count(c => !c.Passed && c.Severity == CheckSeverity.Critical);
            var highSeverityFailures = checks.Count(c => !c.Passed && c.Severity == CheckSeverity.High);
            var mediumSeverityFailures = checks.Count(c => !c.Passed && c.Severity == CheckSeverity.Medium);

            // Critical failures result in technical error
            if (criticalFailures > 0)
                return VerificationStatus.TechnicalError;

            // High fraud risk or multiple high severity failures indicate fraud
            if (metrics.FraudRiskScore > HIGH_RISK_THRESHOLD || highSeverityFailures > 2)
                return VerificationStatus.Fraudulent;

            // Medium fraud risk or quality issues indicate suspicion
            if (metrics.OverallScore < 0.6f ||
                metrics.FraudRiskScore > MEDIUM_RISK_THRESHOLD ||
                highSeverityFailures > 0 ||
                mediumSeverityFailures > 3)
                return VerificationStatus.Suspicious;

            // High overall score with low fraud risk indicates authentic
            if (metrics.OverallScore >= 0.8f && metrics.FraudRiskScore < 0.2f)
                return VerificationStatus.Authentic;

            // Default to pending for borderline cases
            return VerificationStatus.Pending;
        }

        private string GenerateValidationSummary(VerificationMetrics metrics, List<ValidationCheck> checks)
        {
            var passedChecks = checks.Count(c => c.Passed);
            var totalChecks = checks.Count;
            var passRate = totalChecks > 0 ? (float)passedChecks / totalChecks : 0f;

            var summary = $"Validation completed: {passedChecks}/{totalChecks} checks passed ({Math.Round(passRate * 100, 1)}%). ";
            summary += $"Overall score: {Math.Round(metrics.OverallScore * 100, 1)}%. ";

            // Risk assessment
            if (metrics.FraudRiskScore > HIGH_RISK_THRESHOLD)
                summary += "High fraud risk detected - document requires immediate review. ";
            else if (metrics.FraudRiskScore > MEDIUM_RISK_THRESHOLD)
                summary += "Medium fraud risk detected - manual verification recommended. ";
            else
                summary += "Low fraud risk - document appears legitimate. ";

            // Quality assessment
            if (metrics.QualityScore < MIN_QUALITY_THRESHOLD)
                summary += "Image quality issues may affect accuracy. ";
            else if (metrics.QualityScore > 0.8f)
                summary += "Excellent image quality. ";

            // Authenticity assessment
            if (metrics.AuthenticityScore >= 0.8f)
                summary += "Strong authenticity indicators found.";
            else if (metrics.AuthenticityScore >= 0.6f)
                summary += "Moderate authenticity indicators found.";
            else
                summary += "Limited authenticity indicators - document may require additional verification.";

            return summary;
        }

        // Helper methods for complex validations
        private bool ValidateImageMetadata(string filePath)
        {
            // Mock EXIF data validation
            return true; // In production, use image processing library to check EXIF data
        }

        private List<string> ExtractAllNames(string text)
        {
            var names = new List<string>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 3 && trimmedLine.Length < 50 &&
                    !trimmedLine.Any(char.IsDigit) &&
                    Regex.IsMatch(trimmedLine, @"^[A-Za-z\s]+$"))
                {
                    names.Add(trimmedLine);
                }
            }

            return names.Distinct().ToList();
        }

        private List<DateTime> ExtractAllDates(string text)
        {
            var dates = new List<DateTime>();
            var datePattern = @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4}\b";
            var matches = Regex.Matches(text, datePattern);

            foreach (Match match in matches)
            {
                if (DateTime.TryParseExact(match.Value, new[] { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy" },
                    null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    dates.Add(date);
                }
            }

            return dates;
        }

        private List<string> ExtractAllAddresses(string text)
        {
            var addresses = new List<string>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 15 &&
                    (trimmedLine.Contains(",") ||
                     trimmedLine.Contains("street", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("road", StringComparison.OrdinalIgnoreCase) ||
                     Regex.IsMatch(trimmedLine, @"\b\d{6}\b"))) // Contains PIN code
                {
                    addresses.Add(trimmedLine);
                }
            }

            return addresses;
        }

        private string? ExtractStateFromAddress(string address)
        {
            var indianStates = new[]
            {
        "maharashtra", "karnataka", "tamil nadu", "gujarat", "rajasthan",
        "uttar pradesh", "west bengal", "madhya pradesh", "delhi", "bihar"
    };

            foreach (var state in indianStates)
            {
                if (address.Contains(state, StringComparison.OrdinalIgnoreCase))
                    return state;
            }

            return null;
        }

        private float CalculateStringSimilarity(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0f;

            // Simple Levenshtein distance-based similarity
            var maxLength = Math.Max(str1.Length, str2.Length);
            if (maxLength == 0)
                return 1f;

            var distance = LevenshteinDistance(str1.ToLowerInvariant(), str2.ToLowerInvariant());
            return 1f - (float)distance / maxLength;
        }

        private int LevenshteinDistance(string str1, string str2)
        {
            var matrix = new int[str1.Length + 1, str2.Length + 1];

            for (int i = 0; i <= str1.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= str2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[str1.Length, str2.Length];
        }

        private bool IsValidDateString(string dateStr)
        {
            var formats = new[] { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy", "dd MMM yyyy", "MMM dd, yyyy" };
            return DateTime.TryParseExact(dateStr, formats, null, System.Globalization.DateTimeStyles.None, out _);
        }

        private double CalculateFrequencyDifference(double[] expected, double[] actual)
        {
            var difference = 0.0;
            for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            {
                difference += Math.Abs(expected[i] - actual[i]);
            }
            return difference;
        }
        
        private bool ValidateAadhaarStructure(string text)
        {
            var requiredElements = new[]
            {
        "government of india",
        "unique identification",
        @"\b\d{4}\s?\d{4}\s?\d{4}\b" // Aadhaar number pattern
    };

            var foundElements = 0;
            foreach (var element in requiredElements)
            {
                if (Regex.IsMatch(text, element, RegexOptions.IgnoreCase))
                    foundElements++;
            }

            return foundElements >= 2; // At least 2 out of 3 elements should be present
        }

        private bool ValidatePANStructure(string text)
        {
            var requiredElements = new[]
            {
        "income tax",
        "permanent account number",
        @"\b[A-Z]{5}\d{4}[A-Z]\b" // PAN number pattern
    };

            var foundElements = 0;
            foreach (var element in requiredElements)
            {
                if (Regex.IsMatch(text, element, RegexOptions.IgnoreCase))
                    foundElements++;
            }

            return foundElements >= 2;
        }

        private bool ValidatePassportStructure(string text)
        {
            try
            {
                var requiredElements = new[]
                {
                    "republic of india",
                    "passport",
                    "nationality",
                    @"\b[A-Z]\d{7}\b" // Passport number pattern
                };

                var foundElements = 0;
                foreach (var element in requiredElements)
                {
                    if (Regex.IsMatch(text, element, RegexOptions.IgnoreCase))
                        foundElements++;
                }

                return foundElements >= 2;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidatePassportStructure() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool ValidateDrivingLicenseStructure(string text)
        {
            try
            {
                var requiredElements = new[]
                {
                    "driving licen",
                    "transport",
                    "transport",
                    @"\b[A-Z]{2}\d{2}\s?\d{11}\b"
                };

                var foundElements = 0;
                foreach (var element in requiredElements)
                {
                    if (Regex.IsMatch(text, element, RegexOptions.IgnoreCase))
                        foundElements++;
                }

                return foundElements >= 1;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateDrivingLicenseStructure() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool ValidateGenericStructure(string text)
        {
            try
            {
                // Basic structure validation for generic documents
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // Should have reasonable content length and structure
                return text.Length > 50 && lines.Length >= 3 && text.Any(char.IsLetter) && text.Any(char.IsDigit);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateGenericStructure() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
        private async Task<List<ValidationCheck>> ValidateAuthenticityAsync(float authenticityScore, DocumentPatternResult patternResult)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                // Overall authenticity score check
                checks.Add(new ValidationCheck
                {
                    CheckName = "Document_Authenticity",
                    Category = "Authenticity",
                    Passed = authenticityScore >= MIN_AUTHENTICITY_THRESHOLD,
                    Score = authenticityScore,
                    Description = $"Document authenticity score: {Math.Round(authenticityScore * 100, 1)}%",
                    Details = authenticityScore >= 0.8f ? "Strong authenticity indicators" :
                             authenticityScore >= 0.6f ? "Moderate authenticity indicators" : "Weak authenticity indicators",
                    Severity = authenticityScore >= 0.8f ? CheckSeverity.Info :
                              authenticityScore >= 0.6f ? CheckSeverity.Medium : CheckSeverity.High
                });

                // Government authority indicators check
                var hasGovIndicators = CheckGovernmentIndicators(patternResult.OriginalText);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Government_Authority_Indicators",
                    Category = "Authenticity",
                    Passed = hasGovIndicators,
                    Score = hasGovIndicators ? 1.0f : 0.0f,
                    Description = "Government authority markers validation",
                    Details = hasGovIndicators ? "Official government indicators found" : "No government authority markers detected",
                    Severity = hasGovIndicators ? CheckSeverity.Info : CheckSeverity.High
                });

                // Official document format check
                var hasOfficialFormat = CheckOfficialDocumentFormat(patternResult.OriginalText, patternResult.PredictedDocumentType);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Official_Document_Format",
                    Category = "Authenticity",
                    Passed = hasOfficialFormat,
                    Score = hasOfficialFormat ? 1.0f : 0.0f,
                    Description = "Official document format validation",
                    Details = hasOfficialFormat ? "Document follows official format standards" : "Document format does not match official standards",
                    Severity = hasOfficialFormat ? CheckSeverity.Info : CheckSeverity.High
                });

                // Security features check (watermarks, security text, etc.)
                var hasSecurityFeatures = CheckSecurityFeatures(patternResult.OriginalText);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Security_Features",
                    Category = "Authenticity",
                    Passed = hasSecurityFeatures,
                    Score = hasSecurityFeatures ? 1.0f : 0.5f,
                    Description = "Document security features validation",
                    Details = hasSecurityFeatures ? "Security features detected" : "Limited or no security features found",
                    Severity = hasSecurityFeatures ? CheckSeverity.Info : CheckSeverity.Medium
                });

                // Document number validity check
                var hasValidNumbers = ValidateDocumentNumbers(patternResult);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Document_Number_Validity",
                    Category = "Authenticity",
                    Passed = hasValidNumbers,
                    Score = hasValidNumbers ? 1.0f : 0.0f,
                    Description = "Document number format and validity check",
                    Details = hasValidNumbers ? "Document numbers are in valid format" : "Invalid or missing document numbers",
                    Severity = hasValidNumbers ? CheckSeverity.Info : CheckSeverity.High
                });

                // Language and script authenticity
                var hasAuthenticLanguage = CheckLanguageAuthenticity(patternResult.OriginalText, patternResult.PredictedDocumentType);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Language_Script_Authenticity",
                    Category = "Authenticity",
                    Passed = hasAuthenticLanguage,
                    Score = hasAuthenticLanguage ? 1.0f : 0.3f,
                    Description = "Document language and script validation",
                    Details = hasAuthenticLanguage ? "Authentic language patterns detected" : "Unusual language or script patterns",
                    Severity = hasAuthenticLanguage ? CheckSeverity.Info : CheckSeverity.Medium
                });

                // Cross-reference validation (if multiple data points exist)
                var hasConsistentData = CheckDataCrossReference(patternResult);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Data_Cross_Reference",
                    Category = "Authenticity",
                    Passed = hasConsistentData,
                    Score = hasConsistentData ? 1.0f : 0.2f,
                    Description = "Cross-reference data validation",
                    Details = hasConsistentData ? "Data points are consistent across document" : "Inconsistent data points detected",
                    Severity = hasConsistentData ? CheckSeverity.Info : CheckSeverity.High
                });

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateAuthenticityAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private async Task<List<ValidationCheck>> RunFraudDetectionAsync(FraudDetectionInput input)
        {
            try
            {
                await Task.CompletedTask;
                var checks = new List<ValidationCheck>();

                var tamperingScore = 0;
                var tamperingIndicators = new List<string>();

                if (input.HasUnexpectedFonts) { tamperingScore++; tamperingIndicators.Add("Unexpected fonts"); }
                if (input.HasInconsistentSpacing) { tamperingScore++; tamperingIndicators.Add("Inconsistent spacing"); }
                if (input.HasColorAnomalies) { tamperingScore++; tamperingIndicators.Add("Color anomalies"); }
                if (input.HasCompressionArtifacts) { tamperingScore++; tamperingIndicators.Add("Compression artifacts"); }

                var tamperingRisk = tamperingScore / 4.0f;
                checks.Add(new ValidationCheck
                {
                    CheckName = "Tampering_Detection",
                    Category = "Fraud_Detection",
                    Passed = tamperingRisk < 0.5f,
                    Score = 1.0f - tamperingRisk,
                    Description = $"Tampering risk assessment: {Math.Round(tamperingRisk * 100, 1)}%",
                    Details = tamperingIndicators.Count > 0 ? $"Indicators found: {string.Join(", ", tamperingIndicators)}" : "No tampering indicators detected",
                    Severity = tamperingRisk >= 0.7f ? CheckSeverity.Critical : tamperingRisk >= 0.4f ? CheckSeverity.High : CheckSeverity.Info
                });

                var statisticalScore = (input.NumberDistribution + input.TextDistribution + input.ColorDistribution) / 3.0f;
                checks.Add(new ValidationCheck
                {
                    CheckName = "Statistical_Analysis",
                    Category = "Fraud_Detection",
                    Passed = statisticalScore >= 0.6f,
                    Score = statisticalScore,
                    Description = $"Statistical pattern analysis: {Math.Round(statisticalScore * 100, 1)}%",
                    Details = statisticalScore >= 0.8f ? "Normal statistical patterns" : statisticalScore >= 0.6f ? "Minor statistical anomalies" : "Significant statistical anomalies",
                    Severity = statisticalScore >= 0.8f ? CheckSeverity.Info : statisticalScore >= 0.6f ? CheckSeverity.Medium : CheckSeverity.High
                });

                var qualityFraudScore = CalculateQualityFraudScore(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Quality_Based_Fraud_Detection",
                    Category = "Fraud_Detection",
                    Passed = qualityFraudScore < 0.6f,
                    Score = 1.0f - qualityFraudScore,
                    Description = $"Quality-based fraud risk: {Math.Round(qualityFraudScore * 100, 1)}%",
                    Details = GetQualityFraudDetails(input),
                    Severity = qualityFraudScore >= 0.8f ? CheckSeverity.Critical : qualityFraudScore >= 0.6f ? CheckSeverity.High : qualityFraudScore >= 0.3f ? CheckSeverity.Medium : CheckSeverity.Info
                });

                var consistencyFraudScore = CalculateConsistencyFraudScore(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Data_Consistency_Fraud_Detection",
                    Category = "Fraud_Detection",
                    Passed = consistencyFraudScore < 0.5f,
                    Score = 1.0f - consistencyFraudScore,
                    Description = $"Data consistency fraud risk: {Math.Round(consistencyFraudScore * 100, 1)}%",
                    Details = GetConsistencyFraudDetails(input),
                    Severity = consistencyFraudScore >= 0.7f ? CheckSeverity.Critical : consistencyFraudScore >= 0.5f ? CheckSeverity.High : CheckSeverity.Medium
                });

                var structureFraudScore = CalculateStructureFraudScore(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Structure_Based_Fraud_Detection",
                    Category = "Fraud_Detection",
                    Passed = structureFraudScore < 0.4f,
                    Score = 1.0f - structureFraudScore,
                    Description = $"Structure-based fraud risk: {Math.Round(structureFraudScore * 100, 1)}%",
                    Details = GetStructureFraudDetails(input),
                    Severity = structureFraudScore >= 0.6f ? CheckSeverity.High : structureFraudScore >= 0.4f ? CheckSeverity.Medium : CheckSeverity.Info
                });

                var historicalFraudScore = CalculateHistoricalFraudScore(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Historical_Pattern_Analysis",
                    Category = "Fraud_Detection",
                    Passed = historicalFraudScore < 0.3f,
                    Score = 1.0f - historicalFraudScore,
                    Description = $"Historical pattern fraud risk: {Math.Round(historicalFraudScore * 100, 1)}%",
                    Details = GetHistoricalFraudDetails(input),
                    Severity = historicalFraudScore >= 0.5f ? CheckSeverity.High : historicalFraudScore >= 0.3f ? CheckSeverity.Medium : CheckSeverity.Info
                });

                var anomalyScore = RunAdvancedAnomalyDetection(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Advanced_Anomaly_Detection",
                    Category = "Fraud_Detection",
                    Passed = anomalyScore < 0.4f,
                    Score = 1.0f - anomalyScore,
                    Description = $"Advanced anomaly score: {Math.Round(anomalyScore * 100, 1)}%",
                    Details = GetAnomalyDetectionDetails(anomalyScore),
                    Severity = anomalyScore >= 0.6f ? CheckSeverity.Critical : anomalyScore >= 0.4f ? CheckSeverity.High : CheckSeverity.Medium
                });

                var realtimeFraudScore = CheckRealtimeFraudIndicators(input);
                checks.Add(new ValidationCheck
                {
                    CheckName = "Realtime_Fraud_Indicators",
                    Category = "Fraud_Detection",
                    Passed = realtimeFraudScore < 0.3f,
                    Score = 1.0f - realtimeFraudScore,
                    Description = $"Real-time fraud indicators: {Math.Round(realtimeFraudScore * 100, 1)}%",
                    Details = GetRealtimeFraudDetails(input),
                    Severity = realtimeFraudScore >= 0.5f ? CheckSeverity.Critical : realtimeFraudScore >= 0.3f ? CheckSeverity.High : CheckSeverity.Medium
                });

                return checks;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside RunFraudDetectionAsync() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
       
        private bool CheckGovernmentIndicators(string text)
        {
            try
            {
                var govIndicators = new[]
                {
                    "government of india", "govt of india", "भारत सरकार",
                    "republic of india", "भारत गणराज्य",
                    "income tax department", "आयकर विभाग",
                    "unique identification authority", "विशिष्ट पहचान प्राधिकरण",
                    "ministry of", "मंत्रालय",
                    "department of", "विभाग"
                };

                return govIndicators.Any(indicator => text.Contains(indicator, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckGovernmentIndicators() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool CheckOfficialDocumentFormat(string text, DocumentType documentType)
        {
            try
            {
                return documentType switch
                {
                    DocumentType.Aadhaar => text.Contains("आधार", StringComparison.OrdinalIgnoreCase) &&
                                           Regex.IsMatch(text, @"\b\d{4}\s?\d{4}\s?\d{4}\b"),
                    DocumentType.PAN => text.Contains("permanent account number", StringComparison.OrdinalIgnoreCase) &&
                                       Regex.IsMatch(text, @"\b[A-Z]{5}\d{4}[A-Z]\b"),
                    DocumentType.Passport => text.Contains("passport", StringComparison.OrdinalIgnoreCase) &&
                                            text.Contains("republic of india", StringComparison.OrdinalIgnoreCase),
                    _ => text.Length > 50 // Basic format check for other documents
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckOfficialDocumentFormat() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool CheckSecurityFeatures(string text)
        {
            try
            {
                var securityFeatures = new[]
                {
                    "watermark", "security", "hologram", "microprint",
                    "anti-copy", "specimen", "original", "certified"
                };

                return securityFeatures.Any(feature => text.Contains(feature, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckSecurityFeatures() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool ValidateDocumentNumbers(DocumentPatternResult patternResult)
        {
            try
            {
                return patternResult.PredictedDocumentType switch
                {
                    DocumentType.Aadhaar => patternResult.HasAadhaarPattern &&
                                           !string.IsNullOrEmpty(patternResult.AadhaarNumber),
                    DocumentType.PAN => patternResult.HasPANPattern &&
                                       !string.IsNullOrEmpty(patternResult.PANNumber),
                    DocumentType.Passport => patternResult.HasPassportPattern &&
                                            !string.IsNullOrEmpty(patternResult.PassportNumber),
                    _ => true // Other documents may not have specific number patterns
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ValidateDocumentNumbers() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool CheckLanguageAuthenticity(string text, DocumentType documentType)
        {
            try
            {
                var hasEnglish = Regex.IsMatch(text, @"[A-Za-z]+");
                var hasHindi = Regex.IsMatch(text, @"[\u0900-\u097F]+");

                return documentType switch
                {
                    DocumentType.Aadhaar => hasEnglish && hasHindi, // Aadhaar should have both languages
                    DocumentType.PAN => hasEnglish, // PAN is primarily English
                    DocumentType.Passport => hasEnglish, // Passport is primarily English
                    _ => hasEnglish || hasHindi // Other documents can have either
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckLanguageAuthenticity() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private bool CheckDataCrossReference(DocumentPatternResult patternResult)
        {
            try
            {
                var hasName = !string.IsNullOrEmpty(patternResult.OriginalText) && Regex.IsMatch(patternResult.OriginalText, @"[A-Za-z]{3,}");

                var hasNumbers = patternResult.HasAadhaarPattern || patternResult.HasPANPattern || patternResult.HasPassportPattern;

                var hasReasonableLength = patternResult.OriginalText.Length > 100;

                return hasName && hasNumbers && hasReasonableLength;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckDataCrossReference() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        // Helper methods for fraud detection
        private float CalculateQualityFraudScore(FraudDetectionInput input)
        {
            try
            {
                var qualityIssues = 0f;

                if (input.ImageBrightness < 0.2f || input.ImageBrightness > 0.9f) qualityIssues += 0.2f;
                if (input.ImageContrast < 0.3f) qualityIssues += 0.2f;
                if (input.ImageSharpness < 0.4f) qualityIssues += 0.3f;
                if (input.NoiseLevel > 0.4f) qualityIssues += 0.2f;
                if (input.OCRConfidence < 0.6f) qualityIssues += 0.1f;

                return Math.Min(qualityIssues, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateQualityFraudScore() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CalculateConsistencyFraudScore(FraudDetectionInput input)
        {
            try
            {
                var consistencyIssues = 0f;

                if (input.NameConsistency < 0.7f) consistencyIssues += 0.3f;
                if (input.DateConsistency < 0.6f) consistencyIssues += 0.4f;
                if (input.AddressConsistency < 0.5f) consistencyIssues += 0.3f;

                return Math.Min(consistencyIssues, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateConsistencyFraudScore() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CalculateStructureFraudScore(FraudDetectionInput input)
        {
            try
            {
                var structureIssues = 0f;

                if (!input.HasValidNumberFormat) structureIssues += 0.4f;
                if (!input.HasConsistentDateFormats) structureIssues += 0.3f;
                if (!input.HasExpectedDocumentStructure) structureIssues += 0.3f;

                return Math.Min(structureIssues, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateStructureFraudScore() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CalculateHistoricalFraudScore(FraudDetectionInput input)
        {
            try
            {
                var historicalRisk = 0f;

                if (input.UserHistoryScore < 0.5f) historicalRisk += 0.3f;
                if (input.DocumentHistoryScore < 0.6f) historicalRisk += 0.2f;

                // Add file-based historical indicators
                if (input.FileSize < 50000 || input.FileSize > 5000000) historicalRisk += 0.1f; // Unusual file sizes

                return Math.Min(historicalRisk, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CalculateHistoricalFraudScore() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float RunAdvancedAnomalyDetection(FraudDetectionInput input)
        {
            try
            {
                var anomalyScore = 0f;

                // Text length anomalies
                if (input.TextLength < 100 || input.TextLength > 5000) anomalyScore += 0.2f;

                // Language consistency anomalies
                if (input.LanguageConsistency < 0.4f) anomalyScore += 0.3f;

                // Distribution anomalies
                var avgDistribution = (input.NumberDistribution + input.TextDistribution + input.ColorDistribution) / 3.0f;
                if (avgDistribution < 0.5f) anomalyScore += 0.2f;

                // Quality vs confidence mismatch
                var qualityConfidenceDiff = Math.Abs(input.TextQuality - input.OCRConfidence);
                if (qualityConfidenceDiff > 0.3f) anomalyScore += 0.3f;

                return Math.Min(anomalyScore, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside RunAdvancedAnomalyDetection() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private float CheckRealtimeFraudIndicators(FraudDetectionInput input)
        {
            try
            {
                var realtimeRisk = 0f;

                // File extension vs content mismatch
                if (input.FileExtension == ".pdf" && input.TextLength < 200) realtimeRisk += 0.2f;

                // Unusual compression patterns
                if (input.HasCompressionArtifacts) realtimeRisk += 0.3f;

                // Suspicious text patterns
                if (input.TextQuality < 0.5f && input.OCRConfidence > 0.8f) realtimeRisk += 0.2f;

                // Multiple fraud indicators present
                var fraudIndicatorCount = 0;
                if (input.HasUnexpectedFonts) fraudIndicatorCount++;
                if (input.HasInconsistentSpacing) fraudIndicatorCount++;
                if (input.HasColorAnomalies) fraudIndicatorCount++;

                if (fraudIndicatorCount >= 2) realtimeRisk += 0.3f;

                return Math.Min(realtimeRisk, 1.0f);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside CheckRealtimeFraudIndicators() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        // Detail generation methods
        private string GetQualityFraudDetails(FraudDetectionInput input)
        {
            try
            {
                var issues = new List<string>();

                if (input.ImageBrightness < 0.2f) issues.Add("Image too dark");
                if (input.ImageBrightness > 0.9f) issues.Add("Image overexposed");
                if (input.ImageContrast < 0.3f) issues.Add("Low contrast");
                if (input.ImageSharpness < 0.4f) issues.Add("Poor image sharpness");
                if (input.NoiseLevel > 0.4f) issues.Add("High noise level");
                if (input.OCRConfidence < 0.6f) issues.Add("Low OCR confidence");

                return issues.Count > 0 ? string.Join(", ", issues) : "No quality-based fraud indicators";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetQualityFraudDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string GetConsistencyFraudDetails(FraudDetectionInput input)
        {
            try
            {
                var issues = new List<string>();

                if (input.NameConsistency < 0.7f) issues.Add("Name inconsistencies");
                if (input.DateConsistency < 0.6f) issues.Add("Date inconsistencies");
                if (input.AddressConsistency < 0.5f) issues.Add("Address inconsistencies");

                return issues.Count > 0 ? string.Join(", ", issues) : "Data consistency appears normal";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetConsistencyFraudDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string GetStructureFraudDetails(FraudDetectionInput input)
        {
            try
            {
                var issues = new List<string>();

                if (!input.HasValidNumberFormat) issues.Add("Invalid number format");
                if (!input.HasConsistentDateFormats) issues.Add("Inconsistent date formats");
                if (!input.HasExpectedDocumentStructure) issues.Add("Unexpected document structure");

                return issues.Count > 0 ? string.Join(", ", issues) : "Document structure appears valid";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetStructureFraudDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string GetHistoricalFraudDetails(FraudDetectionInput input)
        {
            try
            {
                var issues = new List<string>();

                if (input.UserHistoryScore < 0.5f) issues.Add("Poor user history score");
                if (input.DocumentHistoryScore < 0.6f) issues.Add("Unusual document pattern");

                return issues.Count > 0 ? string.Join(", ", issues) : "Historical patterns appear normal";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetHistoricalFraudDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string GetAnomalyDetectionDetails(float anomalyScore)
        {
            try
            {
                return anomalyScore switch
                {
                    >= 0.8f => "Critical anomalies detected - immediate review required",
                    >= 0.6f => "Significant anomalies detected - manual verification recommended",
                    >= 0.4f => "Minor anomalies detected - additional checks suggested",
                    _ => "No significant anomalies detected"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetAnomalyDetectionDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string GetRealtimeFraudDetails(FraudDetectionInput input)
        {
            try
            {
                var indicators = new List<string>();

                if (input.HasCompressionArtifacts) indicators.Add("Compression artifacts");
                if (input.HasUnexpectedFonts) indicators.Add("Font inconsistencies");
                if (input.HasInconsistentSpacing) indicators.Add("Spacing anomalies");
                if (input.HasColorAnomalies) indicators.Add("Color irregularities");

                return indicators.Count > 0 ? $"Real-time indicators: {string.Join(", ", indicators)}" : "No real-time fraud indicators detected";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GetRealtimeFraudDetails() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }

        private string? ExtractPinCode(string text)
        {
            try
            {
                var pinMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b(\d{6})\b");
                return pinMatch.Success ? pinMatch.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractPinCode() in DocumentValidationService.cs : " + ex);
                throw;
            }
        }
    }
}