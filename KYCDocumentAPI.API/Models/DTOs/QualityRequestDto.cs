namespace KYCDocumentAPI.API.Models.DTOs
{
    public class QualityRequestDto
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }
    }
}
