namespace KYCDocumentAPI.API.Models.Requests
{
    public class TestPatternsRequest
    {
        [Required]
        public string Text { get; set; } = string.Empty;

        public string? FileName { get; set; }
    }
}
