namespace FootballPredictionGame.Models
{
    public class EmailRequest
    {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public byte[] PngBytes { get; set; }
        public byte[] ExcelBytes { get; set; }
        public string FileName { get; set; }
    }
}
