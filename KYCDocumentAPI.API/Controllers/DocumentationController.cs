using KYCDocumentAPI.API.Models.Responses;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentationController : ControllerBase
    {
        private readonly ILogger<DocumentationController> _logger;

        public DocumentationController(ILogger<DocumentationController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive API documentation and usage examples.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public ActionResult<ApiResponse<object>> GetApiDocumentation(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentation = DocumentationData.GetDocumentation();

                return Ok(ApiResponse<object>.SuccessResponse(
                    documentation,
                    "API documentation retrieved successfully"
                ));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetApiDocumentation request was cancelled.");
                return BadRequest(ApiResponse<object>.ErrorResponse("Request was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API documentation.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve API documentation."));
            }
        }

        /// <summary>
        /// Get sample test data for API testing.
        /// </summary>
        [HttpGet("samples")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public ActionResult<ApiResponse<object>> GetSampleData()
        {
            try
            {
                var samples = DocumentationData.GetSampleData();

                return Ok(ApiResponse<object>.SuccessResponse(
                    samples,
                    "Sample data and testing guidelines retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sample data.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve sample data."));
            }
        }

        /// <summary>
        /// Get API health and performance metrics.
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public ActionResult<ApiResponse<object>> GetApiMetrics()
        {
            try
            {
                var metrics = DocumentationData.GetApiMetrics();

                return Ok(ApiResponse<object>.SuccessResponse(
                    metrics,
                    "API metrics retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API metrics.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve API metrics."));
            }
        }
    }

    /// <summary>
    /// Static class holding documentation data (moved out of controller for maintainability).
    /// </summary>
    public static class DocumentationData
    {
        public static object GetDocumentation()
        {
            return new
            {
                ProjectInfo = new
                {
                    Name = "KYC Document Management API",
                    Version = "1.0.0",
                    Description = "AI-Powered Indian KYC Document Management System with Advanced Fraud Detection",
                    Author = "Your Name",
                    Technologies = new[] { ".NET 8", "PostgreSQL", "ML.NET", "Swagger", "Entity Framework" },
                    Features = new[]
                    {
                        "AI Document Classification",
                        "OCR Text Extraction",
                        "Fraud Detection",
                        "Real-time Verification",
                        "Analytics Dashboard",
                        "Indian Document Support"
                    }
                },
                ApiEndpoints = new
                {
                    Users = new
                    {
                        BaseUrl = "/api/users",
                        Operations = new[]
                        {
                            "GET /api/users - List all users with pagination",
                            "GET /api/users/{id} - Get specific user details",
                            "POST /api/users - Create new user",
                            "PUT /api/users/{id} - Update user information",
                            "DELETE /api/users/{id} - Delete user"
                        },
                        SampleRequests = new
                        {
                            CreateUser = new
                            {
                                Method = "POST",
                                Url = "/api/users",
                                Body = new
                                {
                                    firstName = "Raj",
                                    lastName = "Patel",
                                    email = "raj.patel@example.com",
                                    phoneNumber = "9876543210",
                                    dateOfBirth = "1990-05-15",
                                    address = "123 MG Road",
                                    city = "Mumbai",
                                    state = "Maharashtra",
                                    pinCode = "400001"
                                }
                            }
                        }
                    },
                    Documents = new
                    {
                        BaseUrl = "/api/documents",
                        Operations = new[]
                        {
                            "POST /api/documents/upload - Upload document for processing",
                            "GET /api/documents/user/{userId} - Get user's documents",
                            "GET /api/documents/{id} - Get document details",
                            "GET /api/documents/{id}/download - Download document file",
                            "POST /api/documents/{id}/verify - Trigger manual verification",
                            "DELETE /api/documents/{id} - Delete document"
                        },
                        SampleRequests = new
                        {
                            UploadDocument = new
                            {
                                Method = "POST",
                                Url = "/api/documents/upload",
                                ContentType = "multipart/form-data",
                                FormData = new
                                {
                                    file = "aadhaar_card.jpg",
                                    documentType = "Aadhaar",
                                    userId = "guid-here",
                                    notes = "Primary identity document"
                                }
                            }
                        }
                    },
                    FraudDetection = new
                    {
                        BaseUrl = "/api/frauddetection",
                        Operations = new[]
                        {
                            "POST /api/frauddetection/validate/{documentId} - Run comprehensive fraud detection",
                            "POST /api/frauddetection/test - Test fraud detection with file upload",
                            "POST /api/frauddetection/tampering/{documentId} - Detect document tampering",
                            "GET /api/frauddetection/analytics - Get fraud detection analytics",
                            "GET /api/frauddetection/capabilities - Get system capabilities"
                        },
                        SampleResponses = new
                        {
                            ValidationResult = new
                            {
                                isValid = true,
                                status = "Authentic",
                                metrics = new
                                {
                                    overallScore = 89.5,
                                    authenticityScore = 92.1,
                                    qualityScore = 87.3,
                                    fraudRiskScore = 12.4
                                },
                                recommendation = "Document is authentic and can be approved."
                            }
                        }
                    },
                    AITesting = new
                    {
                        BaseUrl = "/api/aitest",
                        Operations = new[]
                        {
                            "POST /api/aitest/ocr - Test OCR functionality",
                            "POST /api/aitest/classify - Test document classification",
                            "POST /api/aitest/patterns - Test pattern recognition",
                            "POST /api/aitest/quality - Test image quality analysis",
                            "GET /api/aitest/status - Get AI service status"
                        }
                    },
                    Dashboard = new
                    {
                        BaseUrl = "/api/dashboard",
                        Operations = new[]
                        {
                            "GET /api/dashboard/analytics - Get dashboard analytics",
                            "GET /api/dashboard/health - Get system health status"
                        }
                    }
                },
                UsageExamples = new
                {
                    CompleteWorkflow = new[]
                    {
                        "1. Create user: POST /api/users",
                        "2. Upload document: POST /api/documents/upload",
                        "3. Monitor processing: GET /api/documents/{id}",
                        "4. Run fraud detection: POST /api/frauddetection/validate/{documentId}",
                        "5. View results: GET /api/documents/user/{userId}"
                    },
                    AITesting = new[]
                    {
                        "1. Test OCR: POST /api/aitest/ocr with file upload",
                        "2. Test classification: POST /api/aitest/classify",
                        "3. Test patterns: POST /api/aitest/patterns with text",
                        "4. Check AI status: GET /api/aitest/status"
                    }
                },
                SupportedDocuments = new
                {
                    FullySupported = new[]
                    {
                        new { Type = "Aadhaar", Pattern = "12-digit UID", Features = new[] { "OCR", "Pattern Recognition", "Fraud Detection" } },
                        new { Type = "PAN", Pattern = "ABCDE1234F", Features = new[] { "Format Validation", "Authenticity Check" } },
                        new { Type = "Passport", Pattern = "A1234567", Features = new[] { "Number Extraction", "Expiry Validation" } }
                    },
                    PartiallySupported = new[] { "Driving License", "Voter ID", "Ration Card" },
                    BasicSupport = new[] { "Bank Passbook", "Utility Bill", "Other" }
                },
                ErrorHandling = new
                {
                    StandardErrorResponse = new
                    {
                        success = false,
                        message = "Error description",
                        data = null as object,
                        errors = new[] { "Detailed error messages" }
                    },
                    CommonStatusCodes = new
                    {
                        Status200 = "Success",
                        Status201 = "Created",
                        Status400 = "Bad Request - Invalid input",
                        Status404 = "Not Found - Resource doesn't exist",
                        Status500 = "Internal Server Error"
                    }
                },
                RateLimits = new
                {
                    DocumentUpload = "100 requests per hour per user",
                    FraudDetection = "50 requests per hour per user",
                    Analytics = "200 requests per hour",
                    General = "1000 requests per hour per API key"
                },
                SecurityFeatures = new[]
                {
                    "File type validation",
                    "File size limits (10MB max)",
                    "Secure file storage",
                    "Input sanitization",
                    "SQL injection prevention",
                    "Comprehensive error handling"
                }
            };
        }

        public static object GetSampleData()
        {
            return new
            {
                SampleUsers = new[]
                {
                    new
                    {
                        firstName = "Rajesh",
                        lastName = "Kumar",
                        email = "rajesh.kumar@example.com",
                        phoneNumber = "9876543210",
                        dateOfBirth = "1985-08-15",
                        address = "45 Brigade Road",
                        city = "Bangalore",
                        state = "Karnataka",
                        pinCode = "560001"
                    },
                    new
                    {
                        firstName = "Priya",
                        lastName = "Sharma",
                        email = "priya.sharma@example.com",
                        phoneNumber = "9123456789",
                        dateOfBirth = "1992-03-22",
                        address = "78 CP Road",
                        city = "New Delhi",
                        state = "Delhi",
                        pinCode = "110001"
                    }
                },
                SampleTextForPatternTesting = new
                {
                    AadhaarSample = "Government of India\nआधार\nName: RAJESH KUMAR\nUID: 1234 5678 9012\nDOB: 15/08/1985\nMale\nAddress: 45 Brigade Road, Bangalore\nPIN: 560001",
                    PANSample = "INCOME TAX DEPARTMENT\nGOVT. OF INDIA\nPermanent Account Number\nRAJESH KUMAR\nPAN: ABCDE1234F\nDOB: 15/08/1985\nFather's Name: SURESH KUMAR",
                    PassportSample = "REPUBLIC OF INDIA\nPASSPORT\nPassport No.: A1234567\nName: RAJESH KUMAR\nNationality: INDIAN\nDate of Birth: 15 AUG 1985\nPlace of Birth: BANGALORE\nDate of Issue: 01 JAN 2020\nDate of Expiry: 31 DEC 2029"
                },
                TestingWorkflows = new
                {
                    BasicTesting = new[]
                    {
                        "Create a user using sample data",
                        "Upload a document (name it with document type like 'aadhaar_test.jpg')",
                        "Check document processing status",
                        "Run fraud detection validation",
                        "View dashboard analytics"
                    },
                    AITesting = new[]
                    {
                        "Test OCR with any image file",
                        "Test document classification with named files",
                        "Test pattern recognition with sample text above",
                        "Test image quality with various image types",
                        "Check AI service capabilities"
                    },
                    FraudDetectionTesting = new[]
                    {
                        "Upload normal document - should pass validation",
                        "Upload poor quality image - should flag quality issues",
                        "Upload file with suspicious name - may trigger alerts",
                        "Test tampering detection on various file types",
                        "Review fraud analytics and trends"
                    }
                },
                PerformanceBenchmarks = new
                {
                    ExpectedResponseTimes = new
                    {
                        DocumentUpload = "< 2 seconds",
                        OCRProcessing = "1-3 seconds",
                        FraudDetection = "2-5 seconds",
                        Analytics = "< 1 second"
                    },
                    ThroughputLimits = new
                    {
                        ConcurrentUploads = "Up to 10 simultaneous",
                        HourlyDocuments = "500-1000 documents",
                        DailyCapacity = "10,000+ documents"
                    }
                }
            };
        }

        public static object GetApiMetrics()
        {
            return new
            {
                SystemMetrics = new
                {
                    ApiVersion = "1.0.0",
                    DotNetVersion = Environment.Version.ToString(),
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = GC.GetTotalMemory(false) / (1024 * 1024) + " MB"
                },
                APIEndpointCount = new
                {
                    TotalEndpoints = 25,
                    UserManagement = 5,
                    DocumentManagement = 6,
                    FraudDetection = 5,
                    AITesting = 5,
                    Dashboard = 2,
                    Documentation = 3
                },
                FeatureComplexity = new
                {
                    LinesOfCode = "~3500+",
                    Models = "15+",
                    Services = "8",
                    Controllers = "6",
                    DatabaseTables = "6",
                    MLComponents = "4"
                },
                TechnologyStack = new
                {
                    Backend = ".NET 8 Web API",
                    Database = "PostgreSQL with Entity Framework Core",
                    MachineLearning = "ML.NET with custom models",
                    Documentation = "Swagger/OpenAPI 3.0",
                    Logging = "Serilog with structured logging",
                    Architecture = "Clean Architecture with DI"
                },
                QualityMetrics = new
                {
                    CodeCoverage = "Comprehensive error handling",
                    APIDocumentation = "100% documented endpoints",
                    ValidationRules = "Input validation on all endpoints",
                    SecurityFeatures = "File validation, size limits, sanitization",
                    PerformanceOptimization = "Async operations, efficient queries"
                }
            };
        }
    }
}
