using KYCDocumentAPI.API.Models.DTOs;
using KYCDocumentAPI.API.Models.Requests;
using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Data;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(ApplicationDbContext context, IFileStorageService fileStorageService, IDocumentProcessingService documentProcessingService, ILogger<DocumentsController> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _documentProcessingService = documentProcessingService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a new document for KYC verification
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        public async Task<ActionResult<ApiResponse<DocumentUploadResponse>>> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            try
            {
                // Validate user exists
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return BadRequest(ApiResponse<DocumentUploadResponse>.ErrorResponse("User not found"));
                }

                // Validate file
                if (!_fileStorageService.IsValidFileType(request.File))
                {
                    return BadRequest(ApiResponse<DocumentUploadResponse>.ErrorResponse("Invalid file type"));
                }

                if (!_fileStorageService.IsValidFileSize(request.File))
                {
                    return BadRequest(ApiResponse<DocumentUploadResponse>.ErrorResponse("File size exceeds limit"));
                }

                // Save file
                var subfolder = $"documents/{request.UserId}/{request.DocumentType}";
                var filePath = await _fileStorageService.SaveFileAsync(request.File, subfolder);

                // Create document record
                var document = new Document
                {
                    DocumentType = request.DocumentType,
                    FileName = request.File.FileName,
                    FilePath = filePath,
                    ContentType = request.File.ContentType,
                    FileSize = request.File.Length,
                    Status = DocumentStatus.Uploaded,
                    UserId = request.UserId
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                await _documentProcessingService.ProcessDocumentAsync(document.Id);

                var response = new DocumentUploadResponse
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    DocumentType = document.DocumentType.ToString(),
                    Status = document.Status.ToString(),
                    Message = "Document uploaded successfully and processing started",
                    ProcessingStarted = true
                };

                return Ok(ApiResponse<DocumentUploadResponse>.SuccessResponse(response, "Document uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, ApiResponse<DocumentUploadResponse>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get all documents for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DocumentDto>>>> GetUserDocuments(Guid userId)
        {
            try
            {
                var documents = await _context.Documents
                    .Include(d => d.DocumentData)                    
                    .Where(d => d.UserId == userId)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        ContentType = d.ContentType,
                        FileSize = d.FileSize,
                        Status = d.Status,
                        CreatedAt = d.CreatedAt,
                        ExtractedData = d.DocumentData != null ? new DocumentDataDto
                        {
                            FullName = d.DocumentData.FullName,
                            DateOfBirth = d.DocumentData.DateOfBirth,
                            Gender = d.DocumentData.Gender,
                            AadhaarNumber = d.DocumentData.AadhaarNumber,
                            PANNumber = d.DocumentData.PANNumber,
                            PassportNumber = d.DocumentData.PassportNumber,
                            IssueDate = d.DocumentData.IssueDate,
                            ExpiryDate = d.DocumentData.ExpiryDate,
                            Address = d.DocumentData.Address,
                            City = d.DocumentData.City,
                            State = d.DocumentData.State,
                            PinCode = d.DocumentData.PinCode,
                            ExtractionConfidence = d.DocumentData.ExtractionConfidence
                        } : null                        
                    })
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<DocumentDto>>.SuccessResponse(documents, "Documents retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for user {UserId}", userId);
                return StatusCode(500, ApiResponse<IEnumerable<DocumentDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get a specific document by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DocumentDto>>> GetDocument(Guid id)
        {
            try
            {
                var document = await _context.Documents.Include(d => d.DocumentData).FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)                
                    return NotFound(ApiResponse<DocumentDto>.ErrorResponse("Document not found"));
                

                var documentDto = new DocumentDto
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    FileSize = document.FileSize,
                    Status = document.Status,
                    CreatedAt = document.CreatedAt,
                    ExtractedData = document.DocumentData != null ? new DocumentDataDto
                    {
                        FullName = document.DocumentData.FullName,
                        DateOfBirth = document.DocumentData.DateOfBirth,
                        Gender = document.DocumentData.Gender,
                        AadhaarNumber = document.DocumentData.AadhaarNumber,
                        PANNumber = document.DocumentData.PANNumber,
                        PassportNumber = document.DocumentData.PassportNumber,
                        IssueDate = document.DocumentData.IssueDate,
                        ExpiryDate = document.DocumentData.ExpiryDate,
                        Address = document.DocumentData.Address,
                        City = document.DocumentData.City,
                        State = document.DocumentData.State,
                        PinCode = document.DocumentData.PinCode,
                        ExtractionConfidence = document.DocumentData.ExtractionConfidence
                    } : null                    
                };

                return Ok(ApiResponse<DocumentDto>.SuccessResponse(documentDto, "Document retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, ApiResponse<DocumentDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Download/view a document file
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                if (!await _fileStorageService.FileExistsAsync(document.FilePath))
                {
                    return NotFound("File not found on disk");
                }

                var fileStream = await _fileStorageService.GetFileStreamAsync(document.FilePath);
                return File(fileStream, document.ContentType ?? "application/octet-stream", document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return StatusCode(500, "Error downloading file");
            }
        }        

        /// <summary>
        /// Delete a document
        /// </summary>
        [HttpDelete("documentId/{documentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteDocument(Guid documentId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)                
                    return NotFound(ApiResponse<bool>.ErrorResponse("Document not found"));
                
                // Delete file from storage
                await _fileStorageService.DeleteFileAsync(document.FilePath);

                // Delete from database
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Document deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete all documents of user
        /// </summary>
        [HttpDelete("userId/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAllDocumentsOfUser(Guid userId)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Documents).AsSplitQuery().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("User not found."));
                else if (user.Documents == null || user.Documents.Count == 0)
                    return NotFound(ApiResponse<bool>.ErrorResponse("User has no documents."));
                                
                foreach(var document in user.Documents)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(document.FilePath) && System.IO.File.Exists(document.FilePath))
                            await _fileStorageService.DeleteFileAsync(document.FilePath);
                        _context.Documents.Remove(document);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error occured inside DeleteAllDocumentsOfUser() in DocumentsController.cs while deleting document with documentId " + document.Id + " : " + ex);
                        throw;
                    }
                }                                                            
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Documents of user with userId " + userId + " are deleted successfully."));
            }           
            catch (Exception ex)
            {
                _logger.LogError("Error occured inside DeleteAllDocumentsOfUser() in DocumentsController.cs : " + ex);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Something bad happened."));
            }
        }
    }
}
