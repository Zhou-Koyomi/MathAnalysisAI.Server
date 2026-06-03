using System.Text.Json;
using MathAnalysisAI.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Data.Seed
{
    public static class PromptProfileSeeder
    {
        private const string DefaultVersion = "v3";

        public static Task<int> SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
        {
            return SeedMathAnalysisPromptProfilesAsync(db, cancellationToken);
        }

        public static async Task<int> SeedMathAnalysisPromptProfilesAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
        {
            var courseId = PlatformSeedData.CourseMathAnalysisId;
            var now = DateTime.UtcNow;
            var outputSchemaJson = BuildOutputSchemaJson();

            var seedItems = new List<PromptProfile>
            {
                BuildProfile(courseId, "solve", DefaultVersion, "Math Analysis Solve v3", BuildSystemPrompt("solve"), BuildUserPromptTemplate()),
                BuildProfile(courseId, "review_solution", DefaultVersion, "Math Analysis Review Solution v3", BuildSystemPrompt("review_solution"), BuildUserPromptTemplate()),
                BuildProfile(courseId, "hint", DefaultVersion, "Math Analysis Hint v3", BuildSystemPrompt("hint"), BuildUserPromptTemplate()),
                BuildProfile(courseId, "exam_mode", DefaultVersion, "Math Analysis Exam Mode v3", BuildSystemPrompt("exam_mode"), BuildUserPromptTemplate()),
                BuildProfile(courseId, "concept_explain", DefaultVersion, "Math Analysis Concept Explain v3", BuildSystemPrompt("concept_explain"), BuildUserPromptTemplate())
            };

            foreach (var item in seedItems)
            {
                item.OutputSchemaJson = outputSchemaJson;
                item.CreatedAt = now;
            }

            var inserted = 0;
            foreach (var item in seedItems)
            {
                var exists = await db.PromptProfiles.AnyAsync(
                    x => x.CourseId == item.CourseId && x.Mode == item.Mode && x.Version == item.Version,
                    cancellationToken
                );

                if (exists)
                {
                    continue;
                }

                db.PromptProfiles.Add(item);
                inserted++;
            }

            if (inserted > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            return inserted;
        }

        private static PromptProfile BuildProfile(
            int courseId,
            string mode,
            string version,
            string name,
            string systemPrompt,
            string userPromptTemplate)
        {
            return new PromptProfile
            {
                CourseId = courseId,
                Mode = mode,
                Version = version,
                Name = name,
                IsActive = true,
                SystemPrompt = systemPrompt,
                UserPromptTemplate = userPromptTemplate,
                OutputSchemaJson = "{}"
            };
        }

        private static string BuildSystemPrompt(string mode)
        {
            return $@"你是面向大学生的本科数学学习平台 AI 助教。平台第一阶段聚焦数学分析。
请基于题目、学生解答、课程知识点上下文给出结构化分析结果。
当前分析模式: {mode}。
你必须且只能输出一个 JSON object，并满足以下约束：
1) 不允许输出 Markdown。
2) 不允许输出 code fence。
3) 不允许输出 schema 外字段。
4) 顶层字段必须全部出现，即使为空也必须输出。
5) 字段名必须严格使用 camelCase。
6) 顶层字段必须且只能是：
   course, chapter, problemType, difficulty, knowledgePoints, solutionOverview, standardSolution, studentSolutionReview, mistakeTags, reviewSuggestions, visualization。
7) 类型约束必须满足：
   standardSolution 必须是 array；
   studentSolutionReview 必须是 object；
   knowledgePoints/mistakeTags/reviewSuggestions 必须是 array；
   visualization 必须是 object。
8) 在 review_solution 模式下，只要学生解答存在明显逻辑错误或理由不充分，studentSolutionReview.isCorrect 必须为 false。
9) 不允许在能判断正误时输出 null；只有题目或学生解答信息不足、完全无法判断时才可为 null。";
        }

        private static string BuildUserPromptTemplate()
        {
            return @"{
  ""course"": ""{{course_name}}"",
  ""chapter"": ""{{chapter_name}}"",
  ""analysisMode"": ""{{analysis_mode}}"",
  ""problemText"": ""{{problem_text}}"",
  ""studentSolutionText"": ""{{student_solution_text}}"",
  ""knowledgeContext"": {{knowledge_points_context_json}}
}

请严格按指定 schema 输出且只输出一个 JSON object。
禁止 Markdown、禁止 code fence、禁止额外文字、禁止 schema 外字段。";
        }

        private static string BuildOutputSchemaJson()
        {
            var schema = new
            {
                course = "数学分析",
                chapter = "反常积分",
                problemType = "improper_integral_convergence",
                difficulty = "medium",
                knowledgePoints = new[]
                {
                    "ma.improper_integral.definition",
                    "ma.improper_integral.comparison_test"
                },
                solutionOverview = "...",
                standardSolution = new[]
                {
                    new { step = 1, title = "...", content = "..." }
                },
                studentSolutionReview = new
                {
                    isCorrect = false,
                    mainIssue = "...",
                    logicGaps = new[] { "..." },
                    suggestions = new[] { "..." }
                },
                mistakeTags = new[] { "invalid_convergence_reason" },
                reviewSuggestions = new[] { "..." },
                visualization = new
                {
                    shouldUse = true,
                    engine = "geogebra",
                    visualizationType = "function_decay",
                    reason = "...",
                    geoGebraCommands = new[] { "f(x)=1/x^2", "Integral(f,1,10)" },
                    caption = "..."
                }
            };

            return JsonSerializer.Serialize(schema);
        }
    }
}
