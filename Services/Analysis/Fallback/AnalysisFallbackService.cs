using System.Text.Json;
using System.Text.Json.Nodes;
using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.Visualization;

namespace MathAnalysisAI.Server.Services.Analysis.Fallback
{
    public sealed class AnalysisFallbackService : IAnalysisFallbackService
    {
        public void ApplyFallbacks(
            AnalysisResponseDto parsed,
            string analysisMode,
            string problemText,
            string? studentSolutionText,
            string? rawLlmContent,
            string? chapterName)
        {
            if (IsLikelyLegacyFormat(parsed))
            {
                var fallbackParsed = TryMapLegacyResponse(rawLlmContent ?? string.Empty);
                if (fallbackParsed != null)
                {
                    MergeParsed(parsed, fallbackParsed);
                }
            }

            ApplyReviewSolutionFallbacks(
                parsed,
                analysisMode,
                problemText,
                studentSolutionText,
                rawLlmContent,
                !string.IsNullOrWhiteSpace(studentSolutionText));

            ApplyKnowledgePointsFallback(parsed, problemText, chapterName);
        }

        private static void MergeParsed(AnalysisResponseDto target, AnalysisResponseDto source)
        {
            target.Course = source.Course;
            target.Chapter = source.Chapter;
            target.ProblemType = source.ProblemType;
            target.Difficulty = source.Difficulty;
            target.KnowledgePoints = source.KnowledgePoints;
            target.SolutionOverview = source.SolutionOverview;
            target.StandardSolution = source.StandardSolution;
            target.StudentSolutionReview = source.StudentSolutionReview;
            target.MistakeTags = source.MistakeTags;
            target.ReviewSuggestions = source.ReviewSuggestions;
            target.Visualization = source.Visualization;
        }

        private static AnalysisResponseDto? TryMapLegacyResponse(string input)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(input);
            }
            catch
            {
                return null;
            }

            if (root is not JsonObject obj)
            {
                return null;
            }

            var hasLegacyShape = obj["overallAssessment"] != null
                || obj["keyIssues"] != null
                || obj["misconceptionAnalysis"] != null
                || obj["suggestedNextSteps"] != null
                || obj["correctnessScore"] != null;

            if (!hasLegacyShape)
            {
                return null;
            }

            var logicGaps = new List<string>();
            if (obj["keyIssues"] is JsonArray issues)
            {
                foreach (var issue in issues)
                {
                    if (issue is JsonObject issueObj)
                    {
                        var detail = issueObj["detail"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(detail))
                        {
                            logicGaps.Add(detail);
                        }
                    }
                }
            }

            var reviewSuggestions = new List<string>();
            var next = obj["suggestedNextSteps"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(next))
            {
                reviewSuggestions.Add(next);
            }

            bool? isCorrect = null;
            var score = obj["correctnessScore"]?.GetValue<double?>();
            if (score.HasValue)
            {
                isCorrect = score.Value >= 0.8d;
            }

            return new AnalysisResponseDto
            {
                Course = string.Empty,
                Chapter = null,
                ProblemType = "unknown",
                Difficulty = "unknown",
                KnowledgePoints = new List<string>(),
                SolutionOverview = obj["overallAssessment"]?.GetValue<string>() ?? string.Empty,
                StandardSolution = new List<StandardSolutionStepDto>(),
                StudentSolutionReview = new StudentSolutionReviewDto
                {
                    IsCorrect = isCorrect,
                    MainIssue = obj["misconceptionAnalysis"]?.GetValue<string>(),
                    LogicGaps = logicGaps,
                    Suggestions = new List<string>()
                },
                MistakeTags = new List<string>(),
                ReviewSuggestions = reviewSuggestions,
                Visualization = new VisualizationDto
                {
                    ShouldUse = false,
                    Engine = "none",
                    VisualizationType = "none",
                    GeoGebraCommands = new List<string>()
                }
            };
        }

        private static bool IsLikelyLegacyFormat(AnalysisResponseDto parsed)
        {
            var noStandardData =
                string.IsNullOrWhiteSpace(parsed.ProblemType) &&
                (parsed.KnowledgePoints == null || parsed.KnowledgePoints.Count == 0) &&
                (parsed.StandardSolution == null || parsed.StandardSolution.Count == 0) &&
                (parsed.ReviewSuggestions == null || parsed.ReviewSuggestions.Count == 0) &&
                string.IsNullOrWhiteSpace(parsed.SolutionOverview);

            return noStandardData;
        }

        private static void ApplyReviewSolutionFallbacks(
            AnalysisResponseDto parsed,
            string analysisMode,
            string problemText,
            string? studentSolutionText,
            string? rawLlmContent,
            bool hasStudentSolution)
        {
            if (analysisMode != "review_solution" || !hasStudentSolution)
            {
                return;
            }

            parsed.StudentSolutionReview ??= new StudentSolutionReviewDto
            {
                IsCorrect = null,
                MainIssue = null,
                LogicGaps = new List<string>(),
                Suggestions = new List<string>()
            };
            parsed.MistakeTags ??= new List<string>();
            parsed.ReviewSuggestions ??= new List<string>();

            if (parsed.StudentSolutionReview.IsCorrect == null)
            {
                var hasIssue = !string.IsNullOrWhiteSpace(parsed.StudentSolutionReview.MainIssue);
                var hasLogicGaps = parsed.StudentSolutionReview.LogicGaps.Count > 0;
                var hasMistakeTags = parsed.MistakeTags.Count > 0;
                var hasReviewHints = ContainsCorrectionHint(parsed.ReviewSuggestions);
                var legacyNegative = LooksLegacyNegative(rawLlmContent);
                var improperIntegralWeakReason = LooksImproperIntegralWeakReason(problemText, studentSolutionText);

                if (hasIssue || hasLogicGaps || hasMistakeTags || hasReviewHints || legacyNegative || improperIntegralWeakReason)
                {
                    parsed.StudentSolutionReview.IsCorrect = false;
                }
            }

            var isImproperIntegral = IsImproperIntegralContext(problemText, parsed.Chapter);
            var usedTendsToZero = ContainsTendsToZeroReason(studentSolutionText);
            if (isImproperIntegral && usedTendsToZero)
            {
                if (string.IsNullOrWhiteSpace(parsed.StudentSolutionReview.MainIssue))
                {
                    parsed.StudentSolutionReview.MainIssue = "被积函数趋于 0 不是反常积分收敛的充分条件；还需要比较判别、p 积分判别或直接计算积分。";
                }

                if (parsed.StudentSolutionReview.LogicGaps == null || parsed.StudentSolutionReview.LogicGaps.Count == 0)
                {
                    parsed.StudentSolutionReview.LogicGaps = new List<string>
                    {
                        "将被积函数趋于 0 误当作反常积分收敛的充分条件。"
                    };
                }
            }

            if ((parsed.ReviewSuggestions == null || parsed.ReviewSuggestions.Count == 0) && isImproperIntegral)
            {
                parsed.ReviewSuggestions = new List<string>
                {
                    "建议使用 p 积分判别或直接计算 ∫_1^∞ 1/x^2 dx；同时对比 ∫_1^∞ 1/x dx，理解“趋于 0”并不保证收敛。"
                };
            }
        }

        private static bool ContainsCorrectionHint(List<string> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
            {
                return false;
            }

            var merged = string.Join(" ", suggestions).ToLowerInvariant();
            return merged.Contains("建议")
                || merged.Contains("纠错")
                || merged.Contains("应当")
                || merged.Contains("需要")
                || merged.Contains("please")
                || merged.Contains("should")
                || merged.Contains("revise");
        }

        private static bool LooksLegacyNegative(string? rawLlmContent)
        {
            if (string.IsNullOrWhiteSpace(rawLlmContent))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawLlmContent);
                var root = doc.RootElement;
                var hasLegacy = root.ValueKind == JsonValueKind.Object
                    && (root.TryGetProperty("overallAssessment", out _)
                        || root.TryGetProperty("keyIssues", out _)
                        || root.TryGetProperty("misconceptionAnalysis", out _));

                if (!hasLegacy)
                {
                    return false;
                }

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("correctnessScore", out var score))
                {
                    if (score.ValueKind == JsonValueKind.Number && score.GetDouble() < 0.8d)
                    {
                        return true;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksImproperIntegralWeakReason(string problemText, string? studentSolutionText)
        {
            if (!IsImproperIntegralContext(problemText, null))
            {
                return false;
            }

            return ContainsTendsToZeroReason(studentSolutionText);
        }

        private static bool ContainsTendsToZeroReason(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var value = text.ToLowerInvariant();
            return value.Contains("趋于0")
                || value.Contains("趋于 0")
                || value.Contains("极限为0")
                || value.Contains("极限为 0")
                || value.Contains("tends to 0");
        }

        private static bool IsImproperIntegralContext(string problemText, string? chapterName)
        {
            var chapter = chapterName ?? string.Empty;
            var text = problemText ?? string.Empty;
            return chapter.Contains("反常积分", StringComparison.OrdinalIgnoreCase)
                || text.Contains("∞", StringComparison.Ordinal)
                || text.Contains("无穷", StringComparison.OrdinalIgnoreCase)
                || text.Contains("反常积分", StringComparison.OrdinalIgnoreCase)
                || text.Contains("∫_1^∞", StringComparison.Ordinal)
                || text.Contains("∫1^∞", StringComparison.Ordinal);
        }

        private static void ApplyKnowledgePointsFallback(AnalysisResponseDto parsed, string problemText, string? chapterName)
        {
            if (parsed.KnowledgePoints != null && parsed.KnowledgePoints.Count > 0)
            {
                return;
            }

            var chapter = chapterName ?? string.Empty;
            var text = problemText ?? string.Empty;
            var looksImproperIntegral = chapter.Contains("反常积分", StringComparison.OrdinalIgnoreCase)
                || text.Contains("∞", StringComparison.Ordinal)
                || text.Contains("无穷", StringComparison.OrdinalIgnoreCase)
                || text.Contains("反常积分", StringComparison.OrdinalIgnoreCase)
                || text.Contains("∫_1^∞", StringComparison.Ordinal)
                || text.Contains("∫1^∞", StringComparison.Ordinal)
                || text.Contains("积分", StringComparison.OrdinalIgnoreCase) && text.Contains("∞", StringComparison.Ordinal);

            if (!looksImproperIntegral)
            {
                return;
            }

            parsed.KnowledgePoints = new List<string>
            {
                "ma.improper_integral.definition",
                "ma.improper_integral.comparison_test"
            };
        }
    }
}
