using KYCDocumentAPI.API.Models.Responses;
using KYCDocumentAPI.Core.Enums;
using KYCDocumentAPI.Infrastructure.Data;
using System.Diagnostics;

namespace KYCDocumentAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard analytics and statistics
        /// </summary>
        [HttpGet("analytics")]
        public async Task<ActionResult<ApiResponse<DashboardResponse>>> GetDashboardAnalytics()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalDocuments = await _context.Documents.CountAsync();
                var verifiedDocuments = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.Verified);
                var pendingVerifications = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.Processing);
                var fraudulentDocuments = await _context.VerificationResults
                    .CountAsync(v => v.Status == VerificationStatus.Fraudulent);

                // Document type distribution
                var documentTypeDistribution = await _context.Documents
                    .GroupBy(d => d.DocumentType)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);

                // State distribution
                var stateDistribution = await _context.Users
                    .Where(u => u.State != null)
                    .GroupBy(u => u.State)
                    .Select(g => new { State = g.Key!.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.State, x => x.Count);

                // Recent activities
                var recentActivities = await _context.Documents
                    .Include(d => d.User)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(10)
                    .Select(d => new RecentActivity
                    {
                        Activity = "Document Upload",
                        UserName = $"{d.User.FirstName} {d.User.LastName}",
                        DocumentType = d.DocumentType.ToString(),
                        Timestamp = d.CreatedAt,
                        Status = d.Status.ToString()
                    })
                    .ToListAsync();

                var dashboardResponse = new DashboardResponse
                {
                    TotalUsers = totalUsers,
                    TotalDocuments = totalDocuments,
                    VerifiedDocuments = verifiedDocuments,
                    PendingVerifications = pendingVerifications,
                    FraudulentDocuments = fraudulentDocuments,
                    DocumentTypeDistribution = documentTypeDistribution,
                    StateDistribution = stateDistribution,
                    RecentActivities = recentActivities
                };

                return Ok(ApiResponse<DashboardResponse>.SuccessResponse(dashboardResponse, "Dashboard data retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard analytics");
                return StatusCode(500, ApiResponse<DashboardResponse>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get system health status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<object>>> GetSystemHealth()
        {
            try
            {
                // Check database connectivity
                var dbConnected = await _context.Database.CanConnectAsync();

                // Get system stats
                var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024); // MB
                var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

                var healthStatus = new
                {
                    Status = dbConnected ? "Healthy" : "Unhealthy",
                    Database = dbConnected ? "Connected" : "Disconnected",
                    MemoryUsageMB = memoryUsage,
                    UptimeHours = Math.Round(uptime.TotalHours, 2),
                    Timestamp = DateTime.UtcNow,
                    Services = new
                    {
                        FileStorage = "Active",
                        DocumentProcessing = "Active",
                        MLModels = "Not Loaded" // Will update this when we add ML.NET
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(healthStatus, "System health retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system health");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("System health check failed"));
            }
        }
    }
}
