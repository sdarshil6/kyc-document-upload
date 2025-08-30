using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Core.Extensions;
using KYCDocumentAPI.Infrastructure.Data;
using KYCDocumentAPI.Infrastructure.DTOs;
using KYCDocumentAPI.Infrastructure.Models;
using KYCDocumentAPI.ML.Models;
using KYCDocumentAPI.ML.Services;
using Microsoft.EntityFrameworkCore;
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
        public DocumentProcessingService(ApplicationDbContext context, ILogger<DocumentProcessingService> logger, IDocumentClassificationService classificationService, IOCRService ocrService, ITextPatternService textPatternService)
        {
            _context = context;
            _logger = logger;
            _classificationService = classificationService;
            _ocrService = ocrService;
            _textPatternService = textPatternService;         
        }

        private async Task<ImagePrediction> ClassifyDocumentAsync(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var result = await _classificationService.ClassifyDocumentAsync(filePath);

                _logger.LogInformation("Document {FilePath} classified as {DocumentType} with confidence {Confidence}", filePath, result.PredictedLabel, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying document {FilePath} inside ClassifyDocumentAsync() in DocumentProcessingService.cs ", filePath);
                throw;
            }
        }

        public async Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType)
        {
            try
            {
                _logger.LogInformation("Extracting data from {DocumentType} document: {FilePath}", documentType, filePath);
                     
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
              
                var patternResult = _textPatternService.AnalyzeText(ocrResult.ExtractedText, Path.GetFileName(filePath));

                var result = new DocumentExtractionResult
                {
                    Success = true,
                    ExtractionConfidence = ocrResult.OverallConfidence * 0.7 + patternResult.Confidence * 0.3,
                    RawData = ocrResult.ExtractedText
                };
                
                switch (documentType)
                {
                    case DocumentType.AadhaarRegular:
                        ExtractAadhaarData(result, patternResult);
                        break;

                    case DocumentType.AadhaarFront:
                        ExtractAadhaarData(result, patternResult);
                        break;

                    case DocumentType.AadhaarBack:
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

                _logger.LogInformation("Data extraction completed for {DocumentType} with confidence {Confidence}", documentType, result.ExtractionConfidence);

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

        public async Task<ProcessedDocumentDto> ProcessDocumentAsync(Guid documentId)
        {
            var processedDocument = new ProcessedDocumentDto();

            try
            {
                var document = await _context.Documents.Include(d => d.DocumentData).FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found for processing", documentId);

                    return new ProcessedDocumentDto
                    {
                        Document = null,
                        DocumentData = null,
                        IsRejected = true,
                        Message = "Document not found"
                    };
                }

                document.Status = DocumentStatus.Processing;
                await _context.SaveChangesAsync();
               
                var classificationResult = await ClassifyDocumentAsync(document.FilePath);

                if (classificationResult.IsConfident() && classificationResult.PredictedLabel != document.DocumentType.GetDescription() && EnumExtensions.TryParseWithSpaces(classificationResult.PredictedLabel, out DocumentType parsedType))
                {
                    _logger.LogInformation("Document {DocumentId} type updated from {OldType} to {NewType}",documentId, document.DocumentType, parsedType);
                    document.DocumentType = parsedType;
                }
                
                var extractionResult = await ExtractDocumentDataAsync(document.FilePath, document.DocumentType);

                if (extractionResult.Success)
                {
                    var documentData = document.DocumentData ?? new DocumentData { DocumentId = documentId };

                    documentData.FullName = extractionResult.FullName;
                    documentData.DateOfBirth = extractionResult.DateOfBirth;
                    documentData.Gender = extractionResult.Gender;
                    documentData.AadhaarNumber = extractionResult.AadhaarNumber;
                    documentData.PANNumber = extractionResult.PANNumber;
                    documentData.PassportNumber = extractionResult.PassportNumber;                    
                    documentData.Address = extractionResult.Address?.Trim().Substring(0, Math.Min(2000, extractionResult.Address.Trim().Length));
                    documentData.City = extractionResult.City;
                    documentData.State = extractionResult.State;
                    documentData.PinCode = extractionResult.PinCode;
                    documentData.ExtractionConfidence = extractionResult.ExtractionConfidence;
                    documentData.RawExtractedData = extractionResult.RawData;

                    if (document.DocumentData == null)
                        _context.DocumentData.Add(documentData);

                    document.DocumentData = documentData;
                }               
                document.Status = DocumentStatus.ClassifiedAndExtracted;

                await _context.SaveChangesAsync();

                processedDocument.Document = document;
                processedDocument.DocumentData = document.DocumentData;
                processedDocument.IsRejected = false;
                processedDocument.Message = "Processed successfully";

                _logger.LogInformation("Document {DocumentId} processed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", documentId);

                var document = await _context.Documents.FindAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Rejected;
                    await _context.SaveChangesAsync();

                    processedDocument.Document = document;
                }

                processedDocument.DocumentData = null;
                processedDocument.IsRejected = true;
                processedDocument.Message = ex.Message;
            }

            return processedDocument;
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
        
    }
}