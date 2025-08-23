namespace KYCDocumentAPI.API.Models.Requests
{
    public class TestTesseractDirectRequest
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }        
    }
}
