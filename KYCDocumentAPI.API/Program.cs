using KYCDocumentAPI.API.Middleware;
using KYCDocumentAPI.Infrastructure.Data;
using KYCDocumentAPI.ML.OCR.Services;
using KYCDocumentAPI.ML.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);



Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KYC-Document-API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "wwwroot", "logs", "kyc-api-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddCors();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("file-storage", () =>
    {
        var uploadPath = builder.Configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
        return Directory.Exists(uploadPath) ? HealthCheckResult.Healthy("File storage accessible") : HealthCheckResult.Unhealthy("File storage not accessible");
    });


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});


builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new KYCDocumentAPI.ML.OCR.Models.OCRConfiguration
    {
        TesseractPath = configuration["OCRSettings:TesseractPath"] ?? "tesseract",
        TesseractDataPath = configuration["OCRSettings:TesseractDataPath"] ?? "",     
        DefaultLanguages = configuration.GetSection("OCRSettings:DefaultLanguages").Get<List<string>>() ?? new List<string> { "eng", "hin", "guj" },
        ProcessingTimeout = configuration.GetValue<int>("OCRSettings:ProcessingTimeout", 30000),
        MaxRetries = configuration.GetValue<int>("OCRSettings:MaxRetries", 3),
        PreprocessImages = configuration.GetValue<bool>("OCRSettings:PreprocessImages", true),
        EnableParallelProcessing = configuration.GetValue<bool>("OCRSettings:EnableParallelProcessing", true),
        CacheResults = configuration.GetValue<bool>("OCRSettings:CacheResults", true)
    };
});


builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<ITextPatternService, TextPatternService>();
builder.Services.AddScoped<IDocumentClassificationService, DocumentClassificationService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddSingleton<TesseractOCREngine>();
builder.Services.AddScoped<IOCREngineFactory, OCREngineFactory>();
builder.Services.AddScoped<IEnhancedOCRService, EnhancedOCRService>();
builder.Services.AddScoped<IOCRService, ProductionOCRService>();
builder.Services.AddScoped<ITrainingDataService, TrainingDataService>();
builder.Services.AddScoped<IMLModelTrainingService, MLModelTrainingService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KYC Document Management API",
        Version = "v1.0.0",
        Description = "AI-Powered Indian KYC Document Management System",
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
        },
    });

    c.OperationFilter<SwaggerFileOperationFilter>();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.EnableAnnotations();
});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/Backend/swagger.json", "Backend");
    });    
}
else
    app.UseHttpsRedirection();

var customWebRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "frontend");
var fileProvider = new PhysicalFileProvider(customWebRootPath);
app.MapFallbackToFile("/index.html", new StaticFileOptions { FileProvider = fileProvider });
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });


app.UseHttpsRedirection();
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");
app.UseRouting();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();


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
}


Log.Information("=== KYC Document API Starting ===");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("Version: 1.0.0");
Log.Information("Features: AI Classification, OCR");

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
