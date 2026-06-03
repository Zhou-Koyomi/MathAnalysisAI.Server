using System.Text.Json;
using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.LLM;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Analysis.Persistence
{
    public sealed class AnalysisPersistenceService : IAnalysisPersistenceService
    {
        private readonly ApplicationDbContext _db;

        public AnalysisPersistenceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(Problem Problem, StudentSolution? StudentSolution)> CreateProblemAggregateAsync(
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            CancellationToken cancellationToken)
        {
            var problem = new Problem
            {
                CourseId = request.CourseId,
                ChapterId = chapter?.Id,
                Title = null,
                ContentMarkdown = request.ProblemText.Trim(),
                ContentLatex = null,
                SourceType = "text",
                SourceFilePath = null,
                ProblemType = "mixed",
                Difficulty = null,
                CreatedByUserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Problems.Add(problem);
            await _db.SaveChangesAsync(cancellationToken);

            StudentSolution? studentSolution = null;
            if (!string.IsNullOrWhiteSpace(request.StudentSolutionText))
            {
                studentSolution = new StudentSolution
                {
                    ProblemId = problem.Id,
                    UserId = request.UserId,
                    SolutionText = request.StudentSolutionText.Trim(),
                    SubmittedAt = DateTime.UtcNow
                };

                _db.StudentSolutions.Add(studentSolution);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return (problem, studentSolution);
        }

        public async Task<AnalysisResult> CreatePendingAnalysisResultAsync(
            Problem problem,
            StudentSolution? studentSolution,
            string analysisMode,
            Course course,
            Chapter? chapter,
            CancellationToken cancellationToken)
        {
            var entity = new AnalysisResult
            {
                ProblemId = problem.Id,
                StudentSolutionId = studentSolution?.Id,
                AnalysisMode = analysisMode,
                CourseName = course.Name,
                ChapterName = chapter?.Name,
                ProblemType = "unknown",
                Difficulty = "unknown",
                KnowledgePointsJson = "[]",
                StandardSolution = null,
                StudentSolutionReview = null,
                MistakeTagsJson = "[]",
                ReviewSuggestionsJson = "[]",
                RawResponseJson = null,
                AiJudgedCorrect = null,
                FinalCorrect = null,
                FinalCorrectSource = CorrectnessSource.Ai,
                CreatedAt = DateTime.UtcNow
            };

            _db.AnalysisResults.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<AnalysisResult> SaveLlmFailedAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            CancellationToken cancellationToken)
        {
            var errorPayload = JsonSerializer.Serialize(new
            {
                status = "llm_failed",
                error_code = llmResponse.ErrorCode,
                error_message = llmResponse.ErrorMessage,
                latency_ms = llmResponse.LatencyMs
            });

            pending.RawResponseJson = errorPayload;
            pending.ProblemType = "unknown";
            pending.Difficulty = "unknown";
            pending.KnowledgePointsJson = "[]";
            pending.StandardSolution = null;
            pending.StudentSolutionReview = null;
            pending.MistakeTagsJson = "[]";
            pending.ReviewSuggestionsJson = "[]";
            pending.AiJudgedCorrect = null;
            pending.FinalCorrect = null;
            await _db.SaveChangesAsync(cancellationToken);
            return pending;
        }

        public async Task<AnalysisResult> SaveParseFailedAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            string parseError,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new
            {
                rawContent = llmResponse.Content,
                parseError,
                latencyMs = llmResponse.LatencyMs,
                tokenUsage = new
                {
                    prompt = llmResponse.PromptTokenCount,
                    completion = llmResponse.CompletionTokenCount,
                    total = llmResponse.TotalTokenCount
                }
            });

            pending.RawResponseJson = payload;
            pending.ProblemType = "unknown";
            pending.Difficulty = "unknown";
            pending.KnowledgePointsJson = "[]";
            pending.StandardSolution = null;
            pending.StudentSolutionReview = null;
            pending.MistakeTagsJson = "[]";
            pending.ReviewSuggestionsJson = "[]";
            pending.AiJudgedCorrect = null;
            pending.FinalCorrect = null;
            await _db.SaveChangesAsync(cancellationToken);
            return pending;
        }

        public async Task<AnalysisResult> SaveSchemaInvalidAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            AnalysisResponseDto parsed,
            string validationError,
            CancellationToken cancellationToken)
        {
            return await SaveParseFailedAsync(
                pending,
                request,
                course,
                chapter,
                llmResponse,
                $"llm_schema_invalid: {validationError}",
                cancellationToken);
        }

        public async Task<AnalysisResult> SaveSuccessAsync(
            AnalysisResult pending,
            AnalysisResponseDto parsed,
            LLMChatResponseDto llmResponse,
            CancellationToken cancellationToken)
        {
            pending.CourseName = parsed.Course;
            pending.ChapterName = parsed.Chapter;
            pending.ProblemType = parsed.ProblemType;
            pending.Difficulty = parsed.Difficulty;
            pending.KnowledgePointsJson = JsonSerializer.Serialize(parsed.KnowledgePoints);
            pending.StandardSolution = JsonSerializer.Serialize(parsed.StandardSolution);
            pending.StudentSolutionReview = JsonSerializer.Serialize(parsed.StudentSolutionReview);
            pending.MistakeTagsJson = JsonSerializer.Serialize(parsed.MistakeTags);
            pending.ReviewSuggestionsJson = JsonSerializer.Serialize(parsed.ReviewSuggestions);
            pending.RawResponseJson = llmResponse.Content;
            pending.AiJudgedCorrect = parsed.StudentSolutionReview.IsCorrect;
            pending.FinalCorrect = parsed.StudentSolutionReview.IsCorrect;
            pending.FinalCorrectSource = CorrectnessSource.Ai;
            await _db.SaveChangesAsync(cancellationToken);
            return pending;
        }
    }
}
