namespace MathAnalysisAI.Models
{
    public class WrongQuestion
    {
        public int Id { get; set; }
        public string ContentHtml { get; set; } = string.Empty;
        public string ErrorCategory { get; set; } = string.Empty;
        public string AIDiagnosis { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}