namespace KYCDocumentAPI.API.Models.Requests
{
    public class BatchProcessRequest
    {
        public List<IFormFile> Files { get; set; }
    }
}
