namespace KYCDocumentAPI.API.Models.DTOs
{
    public class OCRRequestDto
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }
    }
}
