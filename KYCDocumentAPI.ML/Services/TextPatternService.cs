using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.ML.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace KYCDocumentAPI.ML.Services
{
    public class TextPatternService : ITextPatternService
    {
        private readonly ILogger<TextPatternService> _logger;

        // Indian document patterns
        private readonly Regex _aadhaarPattern = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _panPattern = new(@"\b[A-Z]{5}\d{4}[A-Z]\b", RegexOptions.Compiled);
        private readonly Regex _passportPattern = new(@"\b[A-Z]\d{7}\b", RegexOptions.Compiled);        

        // Text indicators for different document types
        private readonly Dictionary<DocumentType, string[]> _documentKeywords = new()
        {
            [DocumentType.AadhaarRegular] = new[] { "aadhaar", "aadhar", "आधार", "uid", "unique identification", "government of india", "male", "female", "dob", "date of birth" },
            [DocumentType.AadhaarFront] = new[] { "aadhaar", "aadhar", "आधार", "uid", "unique identification", "government of india", "male", "female", "dob", "date of birth" },
            [DocumentType.AadhaarBack] = new[] { "aadhaar", "aadhar", "आधार", "uid", "unique identification", "government of india", "male", "female", "dob", "date of birth" },
            [DocumentType.PAN] = new[] { "pan", "permanent account number", "income tax department", "govt of india", "signature", "पैन" },
            [DocumentType.Passport] = new[] { "passport", "republic of india", "nationality", "indian", "place of birth", "पासपोर्ट", "type", "country code" },
            [DocumentType.DrivingLicense] = new[] { "driving license", "driving licence", "dl", "transport", "vehicle", "class", "validity", "ड्राइविंग लाइसेंस" },
            [DocumentType.VoterId] = new[] { "election commission", "voter", "epic", "electors photo identity card", "मतदाता पहचान पत्र" }            
        };

        public TextPatternService(ILogger<TextPatternService> logger)
        {
            _logger = logger;
        }   

        public DocumentPatternResult AnalyzeText(string text, string fileName = "")
        {
            try
            {
                var result = new DocumentPatternResult
                {
                    OriginalText = text,
                    FileName = fileName
                };

                if (string.IsNullOrWhiteSpace(text))
                    return result;

                var normalizedText = text.ToLowerInvariant();
                var normalizedFileName = fileName.ToLowerInvariant();

                // Extract specific patterns
                result.AadhaarNumber = ExtractAadhaarNumber(text);
                result.PANNumber = ExtractPANNumber(text);
                result.PassportNumber = ExtractPassportNumber(text);

                // Calculate confidence scores for each document type
                result.DocumentTypeConfidences = new Dictionary<DocumentType, float>();

                foreach (var docType in _documentKeywords.Keys)
                {
                    float confidence = CalculateDocumentTypeConfidence(normalizedText, normalizedFileName, docType);
                    result.DocumentTypeConfidences[docType] = confidence;
                }

                // Determine the most likely document type
                var bestMatch = result.DocumentTypeConfidences.OrderByDescending(x => x.Value).First();
                result.PredictedDocumentType = bestMatch.Key;
                result.Confidence = bestMatch.Value;

                // Set pattern flags
                result.HasAadhaarPattern = !string.IsNullOrEmpty(result.AadhaarNumber);
                result.HasPANPattern = !string.IsNullOrEmpty(result.PANNumber);
                result.HasPassportPattern = !string.IsNullOrEmpty(result.PassportNumber);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text patterns");
                throw;
            }
        }

        public string ExtractAadhaarNumber(string text)
        {
            try
            {
                var match = _aadhaarPattern.Match(text);
                if (match.Success)
                {
                    var aadhaar = match.Value.Replace(" ", "").Replace("-", "");
                    return IsValidAadhaarFormat(aadhaar) ? aadhaar : string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Aadhaar Number");
                throw;
            }
        }

        public string ExtractPANNumber(string text)
        {
            try
            {
                var match = _panPattern.Match(text);
                if (match.Success)
                {
                    var pan = match.Value.ToUpperInvariant();
                    return IsValidPANFormat(pan) ? pan : string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PAN Number");
                throw;
            }
        }

        public string ExtractPassportNumber(string text)
        {
            try
            {
                var match = _passportPattern.Match(text);
                return match.Success ? match.Value.ToUpperInvariant() : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Passport Number");
                throw;
            }
        }

        public bool IsValidAadhaarFormat(string aadhaar)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aadhaar) || aadhaar.Length != 12)
                    return false;

                // Basic validation - all digits, not all same
                return aadhaar.All(char.IsDigit) && !aadhaar.All(c => c == aadhaar[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inside IsValidAadhaarFormat() in TextPatternService.cs");
                throw;
            }
        }

        public bool IsValidPANFormat(string pan)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pan) || pan.Length != 10)
                    return false;

                // PAN format: 5 letters + 4 digits + 1 letter
                return pan.Take(5).All(char.IsLetter) &&
                       pan.Skip(5).Take(4).All(char.IsDigit) &&
                       char.IsLetter(pan[9]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inside IsValidPANFormat() in TextPatternService.cs");
                throw;
            }
        }

        private float CalculateDocumentTypeConfidence(string text, string fileName, DocumentType documentType)
        {
            try
            {
                var keywords = _documentKeywords[documentType];
                float confidence = 0f;

                // Check text content
                float textScore = 0f;
                foreach (var keyword in keywords)
                {
                    if (text.Contains(keyword))
                    {
                        textScore += 1f;
                    }
                }
                textScore = Math.Min(textScore / keywords.Length, 1f);

                // Check filename
                float fileNameScore = 0f;
                if (!string.IsNullOrEmpty(fileName))
                {
                    foreach (var keyword in keywords.Take(3)) // Use only top keywords for filename
                    {
                        if (fileName.Contains(keyword))
                        {
                            fileNameScore += 0.5f;
                        }
                    }
                    fileNameScore = Math.Min(fileNameScore, 1f);
                }

                // Pattern-specific bonuses
                float patternBonus = 0f;
                switch (documentType)
                {
                    case DocumentType.AadhaarRegular when _aadhaarPattern.IsMatch(text):
                        patternBonus = 0.3f;
                        break;
                    case DocumentType.AadhaarFront when _aadhaarPattern.IsMatch(text):
                        patternBonus = 0.3f;
                        break;
                    case DocumentType.AadhaarBack when _aadhaarPattern.IsMatch(text):
                        patternBonus = 0.3f;
                        break;
                    case DocumentType.PAN when _panPattern.IsMatch(text):
                        patternBonus = 0.3f;
                        break;
                    case DocumentType.Passport when _passportPattern.IsMatch(text):
                        patternBonus = 0.3f;
                        break;
                }

                // Weighted combination
                confidence = (textScore * 0.6f) + (fileNameScore * 0.2f) + (patternBonus * 0.2f);

                return Math.Min(confidence, 1f);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inside CalculateDocumentTypeConfidence() in TextPatternService.cs");
                throw;
            }
        }
    }
}
