using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _basePath;
        private readonly long _maxFileSizeInBytes;
        private readonly string[] _allowedExtensions;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;

           
            _basePath = _configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";

            
            string maxFileSizeStr = _configuration["FileStorage:MaxFileSizeInMB"];
            int maxFileSizeInMB = 10; // default
            if (!string.IsNullOrEmpty(maxFileSizeStr) && int.TryParse(maxFileSizeStr, out int parsed))
            {
                maxFileSizeInMB = parsed;
            }
            _maxFileSizeInBytes = maxFileSizeInMB * 1024 * 1024;

            
            string extensionsStr = _configuration["FileStorage:AllowedExtensions"];
            if (!string.IsNullOrEmpty(extensionsStr))
            {
                _allowedExtensions = extensionsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(e => e.Trim())
                                                  .ToArray();
            }
            else
            {
                _allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            }

            EnsureDirectoryExists(_basePath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subfolder = "")
        {
            try
            {
                if (!IsValidFileType(file))
                    throw new InvalidOperationException($"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");

                if (!IsValidFileSize(file))
                    throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSizeInBytes / (1024 * 1024)}MB");

                var fileName = GenerateUniqueFileName(file.FileName);
                var uploadPath = string.IsNullOrEmpty(subfolder)
                    ? _basePath
                    : Path.Combine(_basePath, subfolder);

                EnsureDirectoryExists(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                var relativePath = Path.Combine(subfolder, fileName).Replace('\\', '/');

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(fileStream);

                _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<Stream> GetFileStreamAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);
                return await Task.FromResult(File.Exists(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            try
            {
                // For development, return relative URL
                // In production, return CDN URL or full server URL
                return $"/uploads/{filePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file url: {FilePath}", filePath);
                throw;
            }
        }

        public bool IsValidFileType(IFormFile file)
        {
            try
            {
                if (file == null) return false;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return _allowedExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inside IsValidFileType() in FileStorageService.cs : " + ex);
                throw;
            }
        }

        public bool IsValidFileSize(IFormFile file)
        {
            try
            {
                if (file == null) return false;

                return file.Length <= _maxFileSizeInBytes && file.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inside IsValidFileSize() in FileStorageService.cs : " + ex);
                throw;
            }
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            try
            {
                var extension = Path.GetExtension(originalFileName);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N")[..8];

                return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inside GenerateUniqueFileName() in FileStorageService.cs : " + ex);
                throw;
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
