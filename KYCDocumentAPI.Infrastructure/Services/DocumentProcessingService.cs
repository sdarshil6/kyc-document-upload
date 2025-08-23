using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Data;
using KYCDocumentAPI.Infrastructure.Models;
using KYCDocumentAPI.ML.Enums;
using KYCDocumentAPI.ML.Models;
using KYCDocumentAPI.ML.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly IDocumentClassificationService _classificationService;
        private readonly IOCRService _ocrService;
        private readonly ITextPatternService _textPatternService;
        private readonly IDocumentValidationService _validationService;        
        public DocumentProcessingService(
            ApplicationDbContext context,
            ILogger<DocumentProcessingService> logger,
            IDocumentClassificationService classificationService,
            IOCRService ocrService,
            ITextPatternService textPatternService,
            IDocumentValidationService validationService)
        {
            _context = context;
            _logger = logger;
            _classificationService = classificationService;
            _ocrService = ocrService;
            _textPatternService = textPatternService;
            _validationService = validationService;
                  }

        private async Task<DocumentClassificationResult> ClassifyDocumentAsync(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var result = await _classificationService.ClassifyDocumentAsync(filePath, fileName);

                _logger.LogInformation("Document {FilePath} classified as {DocumentType} with confidence {Confidence}",
                    filePath, result.PredictedType, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying document {FilePath} inside ClassifyDocumentAsync() in DocumentProcessingService.cs ", filePath);
                return new DocumentClassificationResult
                {
                    PredictedType = DocumentType.Other,
                    Confidence = 0.0,
                    ProcessingNotes = $"Classification failed: {ex.Message}"
                };
            }
        }

        public async Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType)
        {
            try
            {
                _logger.LogInformation("Extracting data from {DocumentType} document: {FilePath}", documentType, filePath);

                // Extract text using OCR               
                var ocrResult = await _ocrService.ExtractTextFromImageAsync(filePath);
                if (!ocrResult.Success)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        Errors = ocrResult.Errors,
                        ExtractionConfidence = 0.0
                    };
                }

                // Analyze patterns in the extracted text
                var patternResult = _textPatternService.AnalyzeText(ocrResult.ExtractedText, Path.GetFileName(filePath));

                var result = new DocumentExtractionResult
                {
                    Success = true,
                    ExtractionConfidence = ocrResult.OverallConfidence * 0.7 + patternResult.Confidence * 0.3,
                    RawData = ocrResult.ExtractedText
                };

                // Extract structured data based on document type and patterns
                switch (documentType)
                {
                    case DocumentType.Aadhaar:
                        ExtractAadhaarData(result, patternResult);
                        break;

                    case DocumentType.PAN:
                        ExtractPANData(result, patternResult);
                        break;

                    case DocumentType.Passport:
                        ExtractPassportData(result, patternResult);
                        break;

                    case DocumentType.DrivingLicense:
                        ExtractDrivingLicenseData(result, patternResult);
                        break;

                    default:
                        ExtractCommonData(result, patternResult);
                        break;
                }

                _logger.LogInformation("Data extraction completed for {DocumentType} with confidence {Confidence}",
                    documentType, result.ExtractionConfidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from document {FilePath} inside ExtractDocumentDataAsync() in DocumentProcessingService.cs ", filePath);
                return new DocumentExtractionResult
                {
                    Success = false,
                    Errors = new List<string> { $"Data extraction failed: {ex.Message}" },
                    ExtractionConfidence = 0.0
                };
            }
        }        

        public async Task ProcessDocumentAsync(Guid documentId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found for processing", documentId);
                    return;
                }

                document.Status = DocumentStatus.Processing;
                await _context.SaveChangesAsync();

                // Classify document type (if not already specified)
                var classificationResult = await ClassifyDocumentAsync(document.FilePath);

                // Update document type if classification is confident and different
                if (classificationResult.IsConfident && classificationResult.PredictedType != document.DocumentType)
                {
                    _logger.LogInformation("Document {DocumentId} type updated from {OldType} to {NewType} based on classification",
                        documentId, document.DocumentType, classificationResult.PredictedType);
                    document.DocumentType = classificationResult.PredictedType;
                }

                // Extract data from document
                var extractionResult = await ExtractDocumentDataAsync(document.FilePath, document.DocumentType);

                if (extractionResult.Success)
                {
                    // Create or update document data
                    var documentData = await _context.DocumentData.FirstOrDefaultAsync(dd => dd.DocumentId == documentId)
                                     ?? new DocumentData { DocumentId = documentId };

                    documentData.FullName = extractionResult.FullName;
                    documentData.DateOfBirth = extractionResult.DateOfBirth;
                    documentData.Gender = extractionResult.Gender;
                    documentData.AadhaarNumber = extractionResult.AadhaarNumber;
                    documentData.PANNumber = extractionResult.PANNumber;
                    documentData.PassportNumber = extractionResult.PassportNumber;
                    documentData.IssueDate = extractionResult.IssueDate;
                    documentData.ExpiryDate = extractionResult.ExpiryDate;
                    documentData.Address = extractionResult.Address;
                    documentData.City = extractionResult.City;
                    documentData.State = extractionResult.State;
                    documentData.PinCode = extractionResult.PinCode;
                    documentData.ExtractionConfidence = extractionResult.ExtractionConfidence;
                    documentData.RawExtractedData = extractionResult.RawData;

                    if (documentData.Id == Guid.Empty)
                    {
                        _context.DocumentData.Add(documentData);
                    }

                    await _context.SaveChangesAsync();
                }                

                _logger.LogInformation("Document {DocumentId} processing completed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId} inside ProcessDocumentAsync() in DocumentProcessingService.cs", documentId);

                // Update document status to reflect error
                var document = await _context.Documents.FindAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Rejected;
                    await _context.SaveChangesAsync();
                }
            }
        }
        
        private void ExtractAadhaarData(DocumentExtractionResult result, DocumentPatternResult patternResult)
        {
            try
            {
                result.AadhaarNumber = patternResult.AadhaarNumber;

                // Extract common information from Aadhaar text
                var text = patternResult.OriginalText;
                result.FullName = ExtractName(text, "aadhaar");
                result.DateOfBirth = ExtractDateOfBirth(text);
                result.Gender = ExtractGender(text);
                result.Address = ExtractAddress(text);
                result.PinCode = ExtractPinCode(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractAadhaarData() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private void ExtractPANData(DocumentExtractionResult result, DocumentPatternResult patternResult)
        {
            try
            {
                result.PANNumber = patternResult.PANNumber;

                var text = patternResult.OriginalText;
                result.FullName = ExtractName(text, "pan");
                result.DateOfBirth = ExtractDateOfBirth(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractPANData() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private void ExtractPassportData(DocumentExtractionResult result, DocumentPatternResult patternResult)
        {
            try
            {
                result.PassportNumber = patternResult.PassportNumber;

                var text = patternResult.OriginalText;
                result.FullName = ExtractName(text, "passport");
                result.DateOfBirth = ExtractDateOfBirth(text);
                result.IssueDate = ExtractIssueDate(text);
                result.ExpiryDate = ExtractExpiryDate(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractPassportData() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private void ExtractDrivingLicenseData(DocumentExtractionResult result, DocumentPatternResult patternResult)
        {
            try
            {
                var text = patternResult.OriginalText;
                result.FullName = ExtractName(text, "license");
                result.DateOfBirth = ExtractDateOfBirth(text);
                result.Address = ExtractAddress(text);
                result.IssueDate = ExtractIssueDate(text);
                result.ExpiryDate = ExtractExpiryDate(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractDrivingLicenseData() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private void ExtractCommonData(DocumentExtractionResult result, DocumentPatternResult patternResult)
        {
            try
            {
                var text = patternResult.OriginalText;
                result.FullName = ExtractName(text, "common");
                result.DateOfBirth = ExtractDateOfBirth(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractCommonData() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private string? ExtractName(string text, string documentType)
        {
            try
            {
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.Length > 3 && trimmedLine.Length < 50 &&
                        !trimmedLine.Any(char.IsDigit) &&
                        !trimmedLine.Contains("government", StringComparison.OrdinalIgnoreCase) &&
                        !trimmedLine.Contains("india", StringComparison.OrdinalIgnoreCase))
                    {
                        // Basic name validation
                        if (Regex.IsMatch(trimmedLine, @"^[A-Za-z\s]+$"))
                        {
                            return trimmedLine;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractName() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private DateTime? ExtractDateOfBirth(string text)
        {
            try
            {
                var dobPatterns = new[]
                    {
                @"\b(?:dob|date of birth)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4})\b",
                @"\b(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4})\b"
            };

                foreach (var pattern in dobPatterns)
                {
                    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dateStr = match.Groups[1].Value;
                        if (DateTime.TryParseExact(dateStr, new[] { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy" },
                            null, System.Globalization.DateTimeStyles.None, out DateTime date))
                        {
                            return date;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractDateOfBirth() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private string? ExtractGender(string text)
        {
            try
            {
                if (text.Contains("male", StringComparison.OrdinalIgnoreCase) &&
                        !text.Contains("female", StringComparison.OrdinalIgnoreCase))
                    return "Male";
                if (text.Contains("female", StringComparison.OrdinalIgnoreCase))
                    return "Female";
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractGender() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private string? ExtractAddress(string text)
        {
            try
            {
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var addressLines = new List<string>();

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.Length > 10 &&
                        (trimmedLine.Contains("street", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.Contains("road", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.Any(char.IsDigit) && trimmedLine.Contains(",")))
                    {
                        addressLines.Add(trimmedLine);
                        if (addressLines.Count >= 2) break; // Limit to 2 lines
                    }
                }

                return addressLines.Count > 0 ? string.Join(", ", addressLines) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractAddress() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private string? ExtractPinCode(string text)
        {
            try
            {
                var pinMatch = Regex.Match(text, @"\b(\d{6})\b");
                return pinMatch.Success ? pinMatch.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractPinCode() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private DateTime? ExtractIssueDate(string text)
        {
            try
            {
                var issueDatePattern = @"(?:issue|issued)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4})";
                var match = Regex.Match(text, issueDatePattern, RegexOptions.IgnoreCase);

                if (match.Success && DateTime.TryParseExact(match.Groups[1].Value,
                    new[] { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy" },
                    null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractIssueDate() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private DateTime? ExtractExpiryDate(string text)
        {
            try
            {
                var expiryDatePattern = @"(?:expiry|expires|valid till)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4})";
                var match = Regex.Match(text, expiryDatePattern, RegexOptions.IgnoreCase);

                if (match.Success && DateTime.TryParseExact(match.Groups[1].Value,
                    new[] { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy" },
                    null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside ExtractExpiryDate() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }

        private string GenerateAIInsights(DocumentValidationResult validationResult)
        {
            try
            {
                var insights = new List<string>();

                // Quality insights
                if (validationResult.Metrics.QualityScore >= 0.8f)
                    insights.Add("Excellent image quality detected");
                else if (validationResult.Metrics.QualityScore < 0.5f)
                    insights.Add("Poor image quality may affect accuracy");

                // Authenticity insights
                if (validationResult.Metrics.AuthenticityScore >= 0.9f)
                    insights.Add("Strong authenticity indicators present");
                else if (validationResult.Metrics.AuthenticityScore < 0.6f)
                    insights.Add("Limited authenticity indicators found");

                // Fraud risk insights
                if (validationResult.Metrics.FraudRiskScore > 0.7f)
                    insights.Add("High fraud risk - multiple red flags detected");
                else if (validationResult.Metrics.FraudRiskScore > 0.4f)
                    insights.Add("Medium fraud risk - some concerns identified");
                else
                    insights.Add("Low fraud risk - document appears legitimate");

                // Specific check insights
                var failedCriticalChecks = validationResult.Checks
                    .Where(c => !c.Passed && c.Severity >= CheckSeverity.High)
                    .ToList();

                if (failedCriticalChecks.Any())
                    insights.Add($"Critical issues: {string.Join(", ", failedCriticalChecks.Select(c => c.CheckName))}");

                // Positive indicators
                if (validationResult.Metrics.PositiveIndicators.Any())
                    insights.Add($"Positive indicators: {string.Join(", ", validationResult.Metrics.PositiveIndicators)}");

                return insights.Any() ? string.Join(". ", insights) + "." : "Document processed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred inside GenerateAIInsights() in DocumentProcessingService.cs : " + ex);
                throw;
            }
        }
    }
}