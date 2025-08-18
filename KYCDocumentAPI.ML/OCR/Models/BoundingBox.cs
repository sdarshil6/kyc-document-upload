namespace KYCDocumentAPI.ML.OCR.Models
{
    /// <summary>
    /// Bounding box coordinates for text elements
    /// </summary>
    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Right => X + Width;
        public int Bottom => Y + Height;
    }
}
