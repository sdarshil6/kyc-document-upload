using Microsoft.AspNetCore.Http;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subfolder = "");
        Task<bool> DeleteFileAsync(string filePath);
        Task<Stream> GetFileStreamAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        string GetFileUrl(string filePath);
        bool IsValidFileType(IFormFile file);
        bool IsValidFileSize(IFormFile file);
        string GenerateUniqueFileName(string originalFileName);
    }
}
