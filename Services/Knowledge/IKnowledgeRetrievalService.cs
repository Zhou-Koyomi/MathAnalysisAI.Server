using MathAnalysisAI.Server.DTOs.Knowledge;

namespace MathAnalysisAI.Server.Services.Knowledge
{
    public interface IKnowledgeRetrievalService
    {
        Task<IReadOnlyList<KnowledgeChunkContextDto>> RetrieveAsync(
            KnowledgeRetrievalRequest request,
            CancellationToken cancellationToken = default);
    }
}
