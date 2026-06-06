namespace MathAnalysisAI.Server.DTOs.Visualization
{
    public class GeoGebraValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> ValidCommands { get; set; } = new();
        public List<string> RejectedCommands { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
