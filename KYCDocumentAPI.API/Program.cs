using KYCDocumentAPI.API.Middleware;
using KYCDocumentAPI.Infrastructure.Data;
using KYCDocumentAPI.ML.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KYC-Document-API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/kyc-api-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Memory Cache
builder.Services.AddMemoryCache();

// Configure Swagger/OpenAPI with enhanced documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KYC Document Management API",
        Version = "v1.0.0",
        Description = @"
            ## AI-Powered Indian KYC Document Management System
            ### Features
            - **AI Document Classification**: Automatically identify document types using ML.NET
            - **OCR Text Extraction**: Extract text from images and PDFs with high accuracy
            - **Fraud Detection**: Advanced algorithms to detect tampering and fraudulent documents
            - **Real-time Verification**: Instant document authenticity checks
            - **Analytics Dashboard**: Comprehensive insights and fraud detection metrics
            - **Indian Document Support**: Specialized handling for Aadhaar, PAN, Passport, etc.
            ### Technology Stack
            - **.NET 8** - High-performance web API
            - **PostgreSQL** - Robust data storage with Entity Framework Core
            - **ML.NET** - Machine learning for document processing
            - **Swagger** - Comprehensive API documentation
            ### Quick Start
            1. Create a user: `POST /api/users`
            2. Upload a document: `POST /api/documents/upload`
            3. Monitor processing: `GET /api/documents/{id}`
            4. Run fraud detection: `POST /api/frauddetection/validate/{documentId}`
            ### Support
            For detailed examples and testing guidelines, visit `/api/documentation`
        ",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Darshil Shah",
            Email = "sdarshil786@gmail.com",
            Url = new Uri("https://github.com/your-repo/kyc-document-api")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add file upload support
    c.OperationFilter<SwaggerFileOperationFilter>();

    // Add XML documentation if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    // Add examples and schemas
    c.EnableAnnotations();
});


// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});


// Register services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IOCRService, MockOCRService>();
builder.Services.AddScoped<ITextPatternService, TextPatternService>();
builder.Services.AddSingleton<IDocumentClassificationService, DocumentClassificationService>();
builder.Services.AddScoped<IDocumentValidationService, DocumentValidationService>();
builder.Services.AddScoped<ICacheService, CacheService>();


// Configure CORS for production
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Add health checks
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>().AddCheck("file-storage", () =>
{
    var uploadPath = builder.Configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
    return Directory.Exists(uploadPath) ? HealthCheckResult.Healthy("File storage accessible") : HealthCheckResult.Unhealthy("File storage not accessible");
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KYC Document API V1");
        c.RoutePrefix = string.Empty; 
        c.DocumentTitle = "KYC Document Management API";
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.ShowExtensions();
    });
}
else
{
    // Production-only middleware
    app.UseHsts();
    app.UseHttpsRedirection();
}


// Add custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Add health check endpoint
app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();

// Database migration and ML service initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
    // Initialize ML services
    var classificationService = scope.ServiceProvider.GetRequiredService<KYCDocumentAPI.ML.Services.IDocumentClassificationService>();
    try
    {
        await classificationService.InitializeModelAsync();
        Log.Information("ML services initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize ML services");
    }
}

// Startup logging
Log.Information("=== KYC Document API Starting ===");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("Version: 1.0.0");
Log.Information("Features: AI Classification, Fraud Detection, OCR, Analytics");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("=== KYC Document API Shutting Down ===");
    Log.CloseAndFlush();
}

// Swagger File Operation Filter for file uploads
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFile[]))
            .ToList();
        if (fileParameters.Any())
        {
            operation.RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
            {
                Content = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiMediaType>
                {
                    ["multipart/form-data"] = new Microsoft.OpenApi.Models.OpenApiMediaType
                    {
                        Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                            {
                                ["file"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File to upload (max 10MB)"
                                }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }
    }
}