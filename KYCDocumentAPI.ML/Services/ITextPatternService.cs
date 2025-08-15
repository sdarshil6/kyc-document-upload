using KYCDocumentAPI.ML.Models;

namespace KYCDocumentAPI.ML.Services
{
    public interface ITextPatternService
    {
        DocumentPatternResult AnalyzeText(string text, string fileName = "");
        string ExtractAadhaarNumber(string text);
        string ExtractPANNumber(string text);
        string ExtractPassportNumber(string text);
        bool IsValidAadhaarFormat(string aadhaar);
        bool IsValidPANFormat(string pan);
    }
}
