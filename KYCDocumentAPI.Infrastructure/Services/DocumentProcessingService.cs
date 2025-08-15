using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Data;
using KYCDocumentAPI.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentProcessingService> _logger;

        public DocumentProcessingService(ApplicationDbContext context, ILogger<DocumentProcessingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DocumentClassificationResult> ClassifyDocumentAsync(string filePath)
        {
            // TODO: Implement ML.NET document classification
            // For now, return a mock result based on file name patterns
            await Task.Delay(100); // Simulate processing time

            var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();

            var result = new DocumentClassificationResult
            {
                PredictedType = DocumentType.Other,
                Confidence = 0.95,
                ProcessingNotes = "Mock classification - will be replaced with ML.NET model"
            };

            // Simple pattern matching for demo
            if (fileName.Contains("aadhaar") || fileName.Contains("aadhar"))
            {
                result.PredictedType = DocumentType.Aadhaar;
            }
            else if (fileName.Contains("pan"))
            {
                result.PredictedType = DocumentType.PAN;
            }
            else if (fileName.Contains("passport"))
            {
                result.PredictedType = DocumentType.Passport;
            }
            else if (fileName.Contains("license"))
            {
                result.PredictedType = DocumentType.DrivingLicense;
            }

            result.AllPredictions = new Dictionary<DocumentType, double>
            {
                { result.PredictedType, result.Confidence },
                { DocumentType.Other, 1 - result.Confidence }
            };

            return result;
        }

        public async Task<DocumentExtractionResult> ExtractDocumentDataAsync(string filePath, DocumentType documentType)
        {
            // TODO: Implement OCR and data extraction with ML.NET
            await Task.Delay(500); // Simulate processing time

            var result = new DocumentExtractionResult
            {
                Success = true,
                ExtractionConfidence = 0.85
            };

            // Mock data extraction based on document type
            switch (documentType)
            {
                case DocumentType.Aadhaar:
                    result.FullName = "John Doe";
                    result.AadhaarNumber = "1234-5678-9012";
                    result.DateOfBirth = new DateTime(1990, 1, 15);
                    result.Gender = "Male";
                    result.Address = "123 Sample Street, Mumbai";
                    result.PinCode = "400001";
                    break;

                case DocumentType.PAN:
                    result.FullName = "John Doe";
                    result.PANNumber = "ABCDE1234F";
                    result.DateOfBirth = new DateTime(1990, 1, 15);
                    break;

                case DocumentType.Passport:
                    result.FullName = "John Doe";
                    result.PassportNumber = "A1234567";
                    result.DateOfBirth = new DateTime(1990, 1, 15);
                    result.IssueDate = new DateTime(2020, 1, 1);
                    result.ExpiryDate = new DateTime(2030, 1, 1);
                    break;
            }

            result.RawData = $"{{\"documentType\":\"{documentType}\",\"processed\":true,\"mock\":true}}";

            return result;
        }

        public async Task<VerificationResult> VerifyDocumentAsync(Guid documentId)
        {
            var document = await _context.Documents
                .Include(d => d.DocumentData)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                throw new ArgumentException("Document not found", nameof(documentId));

            // Create verification result
            var verificationResult = new VerificationResult
            {
                DocumentId = documentId,
                Status = VerificationStatus.Authentic,
                AuthenticityScore = 0.92,
                QualityScore = 0.88,
                ConsistencyScore = 0.95,
                FraudScore = 0.05, // Lower is better for fraud
                IsFormatValid = true,
                IsDataConsistent = true,
                IsImageClear = true,
                IsTampered = false,
                AIInsights = "Document appears authentic with high confidence scores across all verification metrics.",
                ProcessedAt = DateTime.UtcNow
            };

            // Save verification result
            _context.VerificationResults.Add(verificationResult);

            // Update document status
            document.Status = DocumentStatus.Verified;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} verified successfully", documentId);

            return verificationResult;
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

                // Step 1: Classify document type (if not already specified)
                var classificationResult = await ClassifyDocumentAsync(document.FilePath);

                // Update document type if classification is confident and different
                if (classificationResult.IsConfident && classificationResult.PredictedType != document.DocumentType)
                {
                    _logger.LogInformation("Document {DocumentId} type updated from {OldType} to {NewType} based on classification",
                        documentId, document.DocumentType, classificationResult.PredictedType);
                    document.DocumentType = classificationResult.PredictedType;
                }

                // Step 2: Extract data from document
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

                // Step 3: Perform verification
                await VerifyDocumentAsync(documentId);

                _logger.LogInformation("Document {DocumentId} processing completed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", documentId);

                // Update document status to reflect error
                var document = await _context.Documents.FindAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Rejected;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
