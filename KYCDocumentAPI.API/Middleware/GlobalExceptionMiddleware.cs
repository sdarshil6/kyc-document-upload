using KYCDocumentAPI.API.Models.Responses;
using System.Net;
using System.Text.Json;

namespace KYCDocumentAPI.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. RequestId: {RequestId}, Path: {Path}",
                    context.TraceIdentifier, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request.",
                Data = null,
                Errors = new List<string>()
            };

            switch (exception)
            {
                case ArgumentNullException nullEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Required parameter is missing.";
                    response.Errors.Add(nullEx.ParamName ?? "Unknown parameter");
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request parameters.";
                    response.Errors.Add(argEx.Message);
                    break;                

                case FileNotFoundException fileEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Requested file was not found.";
                    response.Errors.Add(fileEx.Message);
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Access denied.";
                    response.Errors.Add("You don't have permission to access this resource.");
                    break;

                case InvalidOperationException opEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid operation.";
                    response.Errors.Add(opEx.Message);
                    break;

                case TimeoutException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout.";
                    response.Errors.Add("The operation took too long to complete.");
                    break;

                case NotSupportedException notSupportedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Operation not supported.";
                    response.Errors.Add(notSupportedEx.Message);
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred.";
                    response.Errors.Add("Please try again later or contact support if the problem persists.");
                    break;
            }

            // Add request context for debugging
            if (context.Response.StatusCode >= 500)
            {
                response.Errors.Add($"RequestId: {context.TraceIdentifier}");
                response.Errors.Add($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
