namespace MathAnalysisAI.Server.DTOs.LLM
{
    public class LLMChatRequestDto
    {
        public string Provider { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public List<LLMChatMessageDto> Messages { get; set; } = new();
        public int? UserId { get; set; }
        public int? AnalysisResultId { get; set; }
    }
}
