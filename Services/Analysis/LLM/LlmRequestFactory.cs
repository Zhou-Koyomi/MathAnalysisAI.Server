using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.AnalysisContext;
using MathAnalysisAI.Server.DTOs.LLM;
using MathAnalysisAI.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Analysis.LLM
{
    public sealed class LlmRequestFactory : ILlmRequestFactory
    {
        private readonly ApplicationDbContext _db;

        public LlmRequestFactory(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<LLMChatRequestDto> BuildAsync(
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            Problem problem,
            StudentSolution? studentSolution,
            AnalysisContextDto? context,
            int analysisResultId,
            CancellationToken cancellationToken)
        {
            var mode = string.IsNullOrWhiteSpace(request.AnalysisMode)
                ? "review_solution"
                : request.AnalysisMode.Trim();

            var promptProfile = await ResolvePromptProfileAsync(request.CourseId, mode, cancellationToken);

            var systemPrompt = promptProfile?.SystemPrompt ??
                "你是本科数学学习平台助教。请按 JSON 返回结构化分析。";

            var template = promptProfile?.UserPromptTemplate ??
                "{\"course\":\"{{course_name}}\",\"chapter\":\"{{chapter_name}}\",\"analysis_mode\":\"{{analysis_mode}}\",\"problem_text\":\"{{problem_text}}\",\"student_solution\":\"{{student_solution_text}}\",\"knowledge_context\":{{knowledge_points_context_json}}}";

            var userPrompt = template
                .Replace("{{course_name}}", EscapeForTemplate(course.Name))
                .Replace("{{chapter_name}}", EscapeForTemplate(chapter?.Name ?? string.Empty))
                .Replace("{{analysis_mode}}", EscapeForTemplate(mode))
                .Replace("{{problem_text}}", EscapeForTemplate(problem.ContentMarkdown))
                .Replace("{{student_solution_text}}", EscapeForTemplate(studentSolution?.SolutionText ?? string.Empty))
                .Replace("{{knowledge_points_context_json}}", "[]");

            var contextBlock = RenderContextBlock(context);
            if (!string.IsNullOrWhiteSpace(contextBlock))
            {
                userPrompt = $"{userPrompt}\n\n{contextBlock}";
            }

            return new LLMChatRequestDto
            {
                Provider = "deepseek",
                ModelName = "deepseek-chat",
                RequestType = string.IsNullOrWhiteSpace(request.AnalysisMode) ? "review_solution" : request.AnalysisMode,
                UserId = request.UserId,
                AnalysisResultId = analysisResultId,
                Messages = new List<LLMChatMessageDto>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
                }
            };
        }

        private async Task<PromptProfile?> ResolvePromptProfileAsync(int courseId, string mode, CancellationToken cancellationToken)
        {
            return await _db.PromptProfiles
                .AsNoTracking()
                .Where(x => x.CourseId == courseId && x.Mode == mode && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Version)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static string EscapeForTemplate(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string RenderContextBlock(AnalysisContextDto? context)
        {
            return string.IsNullOrWhiteSpace(context?.PromptContextBlock)
                ? string.Empty
                : context!.PromptContextBlock.Trim();
        }
    }
}
