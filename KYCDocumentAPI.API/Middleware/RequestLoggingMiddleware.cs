namespace KYCDocumentAPI.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Log request
            _logger.LogInformation("HTTP {Method} {Path} started. RequestId: {RequestId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. RequestId: {RequestId}", context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, context.TraceIdentifier);

                // Log slow requests
                if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
                {
                    _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms. RequestId: {RequestId}", context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds, context.TraceIdentifier);
                }
            }
        }
    }
}
