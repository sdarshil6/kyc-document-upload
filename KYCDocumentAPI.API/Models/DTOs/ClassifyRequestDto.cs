namespace KYCDocumentAPI.API.Models.DTOs
{
    public class ClassifyRequestDto
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }
    }
}
