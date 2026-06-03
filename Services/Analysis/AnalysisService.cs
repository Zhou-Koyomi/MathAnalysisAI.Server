using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.AnalysisContext;
using MathAnalysisAI.Server.DTOs.Visualization;
using MathAnalysisAI.Server.Models;
using MathAnalysisAI.Server.Services.Analysis.Fallback;
using MathAnalysisAI.Server.Services.Analysis.Context;
using MathAnalysisAI.Server.Services.Analysis.LLM;
using MathAnalysisAI.Server.Services.Analysis.Mistakes;
using MathAnalysisAI.Server.Services.Analysis.Parsing;
using MathAnalysisAI.Server.Services.Analysis.Persistence;
using MathAnalysisAI.Server.Services.Analysis.Stats;
using MathAnalysisAI.Server.Services.Knowledge;
using MathAnalysisAI.Server.Services.LLM;
using MathAnalysisAI.Server.Services.Visualization;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Analysis
{
    public class AnalysisService
    {
        private readonly ApplicationDbContext _db;
        private readonly LLMGateway _llmGateway;
        private readonly VisualizationService _visualizationService;
        private readonly ILlmResponseParser _llmResponseParser;
        private readonly IAnalysisFallbackService _analysisFallbackService;
        private readonly IAnalysisPersistenceService _analysisPersistenceService;
        private readonly IMistakeRecordService _mistakeRecordService;
        private readonly IUserStatsUpdateService _userStatsUpdateService;
        private readonly IAnalysisContextBuilder _analysisContextBuilder;
        private readonly ILlmRequestFactory _llmRequestFactory;
        private readonly ILogger<AnalysisService> _logger;

        public AnalysisService(
            ApplicationDbContext db,
            LLMGateway llmGateway,
            VisualizationService visualizationService,
            ILlmResponseParser llmResponseParser,
            IAnalysisFallbackService analysisFallbackService,
            IAnalysisPersistenceService analysisPersistenceService,
            IMistakeRecordService mistakeRecordService,
            IUserStatsUpdateService userStatsUpdateService,
            IAnalysisContextBuilder analysisContextBuilder,
            ILlmRequestFactory llmRequestFactory,
            ILogger<AnalysisService> logger)
        {
            _db = db;
            _llmGateway = llmGateway;
            _visualizationService = visualizationService;
            _llmResponseParser = llmResponseParser;
            _analysisFallbackService = analysisFallbackService;
            _analysisPersistenceService = analysisPersistenceService;
            _mistakeRecordService = mistakeRecordService;
            _userStatsUpdateService = userStatsUpdateService;
            _analysisContextBuilder = analysisContextBuilder;
            _llmRequestFactory = llmRequestFactory;
            _logger = logger;
        }

        public async Task<AnalysisResponseDto> AnalyzeAsync(
            AnalysisRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var response = BuildFallbackResponse();

            var validationError = ValidateRequest(request);
            if (validationError != null)
            {
                response.StudentSolutionReview.MainIssue = validationError;
                return response;
            }

            var normalizedMode = string.IsNullOrWhiteSpace(request.AnalysisMode)
                ? "review_solution"
                : request.AnalysisMode.Trim();

            var course = await _db.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                response.StudentSolutionReview.MainIssue = "Course not found.";
                return response;
            }

            Chapter? chapter = null;
            if (request.ChapterId.HasValue)
            {
                chapter = await _db.Chapters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.ChapterId.Value && x.CourseId == request.CourseId, cancellationToken);
            }

            var aggregate = await _analysisPersistenceService.CreateProblemAggregateAsync(
                request,
                course,
                chapter,
                cancellationToken);
            var problem = aggregate.Problem;
            var studentSolution = aggregate.StudentSolution;

            var pendingAnalysisResult = await _analysisPersistenceService.CreatePendingAnalysisResultAsync(
                problem,
                studentSolution,
                normalizedMode,
                course,
                chapter,
                cancellationToken);

            AnalysisContextDto analysisContext;
            try
            {
                analysisContext = await _analysisContextBuilder.BuildAsync(
                    new AnalysisContextBuildRequest
                    {
                        Request = request,
                        Course = course,
                        Chapter = chapter,
                        Problem = problem,
                        StudentSolution = studentSolution,
                        NormalizedKnowledgePointCodes = null
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AnalysisContextBuilder failed. Falling back to empty context for CourseId={CourseId}, AnalysisMode={AnalysisMode}", request.CourseId, normalizedMode);
                analysisContext = AnalysisContextDto.Empty;
            }

            var llmRequest = await _llmRequestFactory.BuildAsync(
                request,
                course,
                chapter,
                problem,
                studentSolution,
                analysisContext,
                pendingAnalysisResult.Id,
                cancellationToken);
            var llmResponse = await _llmGateway.ChatAsync(llmRequest, cancellationToken);

            if (!llmResponse.IsSuccess)
            {
                var failedResult = await _analysisPersistenceService.SaveLlmFailedAsync(
                    pendingAnalysisResult,
                    request,
                    course,
                    chapter,
                    llmResponse,
                    cancellationToken);

                response.AnalysisResultId = failedResult.Id;
                response.ProblemId = problem.Id;
                response.StudentSolutionId = studentSolution?.Id;
                response.Course = course.Name;
                response.Chapter = chapter?.Name;
                response.StudentSolutionReview.MainIssue = $"LLM failed: {llmResponse.ErrorCode} {llmResponse.ErrorMessage}".Trim();

                await _userStatsUpdateService.UpdateCourseStatsAsync(
                    request.UserId,
                    request.CourseId,
                    _userStatsUpdateService.ResolveCorrectness(failedResult),
                    cancellationToken);
                return response;
            }

            var parseResult = _llmResponseParser.Parse(llmResponse.Content);
            if (!parseResult.Success || parseResult.Parsed == null)
            {
                var parseFailResult = await _analysisPersistenceService.SaveParseFailedAsync(
                    pendingAnalysisResult,
                    request,
                    course,
                    chapter,
                    llmResponse,
                    parseResult.ErrorMessage ?? "Unknown parser error.",
                    cancellationToken);

                response.AnalysisResultId = parseFailResult.Id;
                response.ProblemId = problem.Id;
                response.StudentSolutionId = studentSolution?.Id;
                response.Course = course.Name;
                response.Chapter = chapter?.Name;
                response.StudentSolutionReview.MainIssue = $"JSON parse failed: {parseResult.ErrorMessage}";

                await _userStatsUpdateService.UpdateCourseStatsAsync(
                    request.UserId,
                    request.CourseId,
                    _userStatsUpdateService.ResolveCorrectness(parseFailResult),
                    cancellationToken);
                return response;
            }

            var parsed = parseResult.Parsed ?? BuildFallbackResponse();
            NormalizeParsedResponse(parsed, course.Name, chapter?.Name);
            _analysisFallbackService.ApplyFallbacks(
                parsed,
                normalizedMode,
                request.ProblemText,
                request.StudentSolutionText,
                parseResult.NormalizedJson ?? llmResponse.Content,
                chapter?.Name);
            parsed.KnowledgePoints = await KnowledgePointNormalizer.NormalizeAsync(
                _db,
                parsed.KnowledgePoints,
                request.CourseId,
                request.ChapterId,
                request.ProblemText,
                request.StudentSolutionText,
                cancellationToken);

            var schemaError = ValidateParsedResponse(
                parsed,
                normalizedMode,
                !string.IsNullOrWhiteSpace(request.StudentSolutionText));
            if (schemaError != null)
            {
                var schemaFailResult = await _analysisPersistenceService.SaveSchemaInvalidAsync(
                    pendingAnalysisResult,
                    request,
                    course,
                    chapter,
                    llmResponse,
                    parsed,
                    schemaError,
                    cancellationToken);

                response.AnalysisResultId = schemaFailResult.Id;
                response.ProblemId = problem.Id;
                response.StudentSolutionId = studentSolution?.Id;
                response.Course = course.Name;
                response.Chapter = chapter?.Name;
                response.StudentSolutionReview.MainIssue = $"llm_schema_invalid: {schemaError}";

                await _userStatsUpdateService.UpdateCourseStatsAsync(
                    request.UserId,
                    request.CourseId,
                    _userStatsUpdateService.ResolveCorrectness(schemaFailResult),
                    cancellationToken);
                return response;
            }

            var analysisResult = await _analysisPersistenceService.SaveSuccessAsync(
                pendingAnalysisResult,
                parsed,
                llmResponse,
                cancellationToken);

            parsed.AnalysisResultId = analysisResult.Id;
            parsed.ProblemId = problem.Id;
            parsed.StudentSolutionId = studentSolution?.Id;
            var mistakeKnowledgePointIds = await _mistakeRecordService.SaveMistakeRecordsAsync(
                analysisResult.Id,
                request.CourseId,
                parsed.KnowledgePoints,
                parsed.MistakeTags,
                cancellationToken);
            await _visualizationService.SaveVisualizationAsync(analysisResult.Id, parsed.Visualization, cancellationToken);
            await _userStatsUpdateService.UpdateAfterAnalysisAsync(
                request.UserId,
                studentSolution?.UserId,
                request.CourseId,
                parsed.KnowledgePoints,
                mistakeKnowledgePointIds,
                analysisResult,
                cancellationToken);

            return parsed;
        }

        private static string? ValidateRequest(AnalysisRequestDto request)
        {
            if (request.CourseId <= 0)
            {
                return "CourseId is required.";
            }

            if (string.IsNullOrWhiteSpace(request.ProblemText))
            {
                return "ProblemText is required.";
            }

            if (string.IsNullOrWhiteSpace(request.AnalysisMode))
            {
                request.AnalysisMode = "review_solution";
            }

            return null;
        }

        private static void NormalizeParsedResponse(AnalysisResponseDto response, string courseName, string? chapterName)
        {
            response.Course = string.IsNullOrWhiteSpace(response.Course) ? courseName : response.Course;
            response.Chapter ??= chapterName;
            response.ProblemType = string.IsNullOrWhiteSpace(response.ProblemType) ? "unknown" : response.ProblemType;
            response.Difficulty = string.IsNullOrWhiteSpace(response.Difficulty) ? "unknown" : response.Difficulty;
            response.KnowledgePoints ??= new List<string>();
            response.StandardSolution ??= new List<StandardSolutionStepDto>();
            response.StudentSolutionReview ??= new StudentSolutionReviewDto();
            response.MistakeTags ??= new List<string>();
            response.ReviewSuggestions ??= new List<string>();
            response.Visualization ??= new VisualizationDto
            {
                ShouldUse = false,
                Engine = "none",
                VisualizationType = "none",
                GeoGebraCommands = new List<string>()
            };
            response.SolutionOverview ??= string.Empty;
        }

        private static AnalysisResponseDto BuildFallbackResponse()
        {
            return new AnalysisResponseDto
            {
                Course = string.Empty,
                Chapter = null,
                ProblemType = "unknown",
                Difficulty = "unknown",
                KnowledgePoints = new List<string>(),
                SolutionOverview = string.Empty,
                StandardSolution = new List<StandardSolutionStepDto>(),
                StudentSolutionReview = new StudentSolutionReviewDto
                {
                    IsCorrect = null,
                    MainIssue = null,
                    LogicGaps = new List<string>(),
                    Suggestions = new List<string>()
                },
                MistakeTags = new List<string>(),
                ReviewSuggestions = new List<string>(),
                Visualization = new VisualizationDto
                {
                    ShouldUse = false,
                    Engine = "none",
                    VisualizationType = "none",
                    GeoGebraCommands = new List<string>()
                }
            };
        }

        private static string? ValidateParsedResponse(
            AnalysisResponseDto parsed,
            string analysisMode,
            bool hasStudentSolution)
        {
            if (string.IsNullOrWhiteSpace(parsed.Course))
            {
                return "course is empty";
            }

            if (string.IsNullOrWhiteSpace(parsed.ProblemType) || parsed.ProblemType.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            {
                return "problemType is empty or unknown";
            }

            var hasOverview = !string.IsNullOrWhiteSpace(parsed.SolutionOverview);
            var hasStandard = parsed.StandardSolution != null && parsed.StandardSolution.Count > 0;
            if (!hasOverview && !hasStandard)
            {
                return "solutionOverview and standardSolution are both empty";
            }

            if (parsed.StudentSolutionReview == null)
            {
                return "studentSolutionReview is empty";
            }

            if (analysisMode == "review_solution" && hasStudentSolution && parsed.StudentSolutionReview.IsCorrect == null)
            {
                var explicitUnknown = IsExplicitlyUnableToJudge(parsed.StudentSolutionReview.MainIssue)
                    || IsExplicitlyUnableToJudge(parsed.SolutionOverview);

                if (!explicitUnknown)
                {
                    return "studentSolutionReview.isCorrect is null";
                }
            }

            return null;
        }

        private static bool IsExplicitlyUnableToJudge(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var value = text.ToLowerInvariant();
            return value.Contains("无法判断")
                || value.Contains("不能判断")
                || value.Contains("insufficient")
                || value.Contains("cannot determine")
                || value.Contains("unable to determine");
        }
    }
}
