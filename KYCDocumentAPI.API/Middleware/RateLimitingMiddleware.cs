using KYCDocumentAPI.API.Models.Responses;
using System.Text.Json;

namespace KYCDocumentAPI.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly Dictionary<string, List<DateTime>> _requests = new();
        private static readonly object _lock = new();
        private const int MaxRequestsPerHour = 1000;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;
            bool isRateLimited = false;
            int requestCount = 0;

            lock (_lock)
            {
                if (!_requests.ContainsKey(clientIp))
                {
                    _requests[clientIp] = new List<DateTime>();
                }

                // Remove old requests (older than 1 hour)
                _requests[clientIp].RemoveAll(time => (now - time).TotalHours > 1);

                // Check rate limit
                if (_requests[clientIp].Count >= MaxRequestsPerHour)
                {
                    isRateLimited = true;
                    requestCount = _requests[clientIp].Count;
                }
                else
                {
                    // Add current request
                    _requests[clientIp].Add(now);
                }
            }

            if (isRateLimited)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP: {ClientIp}. Requests in last hour: {RequestCount}", clientIp, requestCount);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later.",
                    Errors = new List<string> { $"Maximum {MaxRequestsPerHour} requests per hour allowed." }
                };

                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
                return;
            }

            await _next(context);
        }
    }
}
