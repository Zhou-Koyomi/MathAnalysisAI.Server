namespace MathAnalysisAI.Server.DTOs.Knowledge
{
    public class KnowledgeChunkContextDto
    {
        public int ChunkId { get; set; }
        public int MaterialId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MaterialKind { get; set; } = string.Empty;
        public string? SectionTitle { get; set; }
        public string? SectionPath { get; set; }
        public int? PageStart { get; set; }
        public int? PageEnd { get; set; }
        public string ChunkType { get; set; } = "unknown";
        public string ContentPreview { get; set; } = string.Empty;
        public List<string> MatchedKnowledgePoints { get; set; } = new();
        public decimal Score { get; set; }
        public string SourceLabel { get; set; } = "sql_keyword";
    }
}
