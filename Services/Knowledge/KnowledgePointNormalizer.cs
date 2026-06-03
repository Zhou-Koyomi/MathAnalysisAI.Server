using MathAnalysisAI.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Knowledge
{
    public static class KnowledgePointNormalizer
    {
        private static readonly Dictionary<string, string[]> LabelToCodeCandidates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["反常积分"] = new[] { "ma.improper_integral.convergence_criteria", "ma.improper_integral.comparison_test" },
            ["反常积分收敛性"] = new[] { "ma.improper_integral.convergence_criteria", "ma.improper_integral.comparison_test" },
            ["反常积分的收敛性判定"] = new[] { "ma.improper_integral.convergence_criteria", "ma.improper_integral.comparison_test" },
            ["无穷限反常积分"] = new[] { "ma.improper_integral.infinite_interval" },
            ["无穷区间反常积分"] = new[] { "ma.improper_integral.infinite_interval" },
            ["p-积分"] = new[] { "ma.improper_integral.convergence_criteria" },
            ["p积分"] = new[] { "ma.improper_integral.convergence_criteria" },
            ["比较判别法"] = new[] { "ma.improper_integral.comparison_test" },
            ["积分判别"] = new[] { "ma.improper_integral.comparison_test", "ma.improper_integral.convergence_criteria" },
            ["ma.improper_integral.convergence_criteria"] = new[] { "ma.improper_integral.convergence_criteria" }
        };

        public static async Task<List<string>> NormalizeAsync(
            ApplicationDbContext db,
            IEnumerable<string>? rawKnowledgePoints,
            int courseId,
            int? chapterId,
            string problemText,
            string? studentSolutionText,
            CancellationToken cancellationToken = default)
        {
            var existingCodes = await db.KnowledgePoints
                .AsNoTracking()
                .Where(x => x.CourseId == courseId && x.Code != null && x.Code != "")
                .Select(x => x.Code!)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
            var output = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in rawKnowledgePoints ?? Enumerable.Empty<string>())
            {
                var label = NormalizeLabel(raw);
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                // Already a valid code.
                if (existingSet.Contains(label))
                {
                    AddCode(label, seen, output);
                    continue;
                }

                if (!LabelToCodeCandidates.TryGetValue(label, out var candidates))
                {
                    continue;
                }

                foreach (var candidate in candidates)
                {
                    if (existingSet.Contains(candidate))
                    {
                        AddCode(candidate, seen, output);
                        break;
                    }
                }
            }

            if (output.Count == 0 && await IsImproperIntegralContextAsync(db, chapterId, problemText, studentSolutionText, cancellationToken))
            {
                foreach (var fallback in ResolveImproperIntegralFallbackCodes(existingSet))
                {
                    AddCode(fallback, seen, output);
                }
            }

            return output;
        }

        private static IEnumerable<string> ResolveImproperIntegralFallbackCodes(HashSet<string> existingSet)
        {
            // Prefer requested code when available; fallback to actually seeded equivalents.
            var preferred = new[]
            {
                "ma.improper_integral.definition",
                "ma.improper_integral.convergence_criteria"
            };
            var first = preferred.FirstOrDefault(existingSet.Contains);
            if (!string.IsNullOrWhiteSpace(first))
            {
                yield return first;
            }

            if (existingSet.Contains("ma.improper_integral.comparison_test"))
            {
                yield return "ma.improper_integral.comparison_test";
            }
        }

        private static async Task<bool> IsImproperIntegralContextAsync(
            ApplicationDbContext db,
            int? chapterId,
            string problemText,
            string? studentSolutionText,
            CancellationToken cancellationToken)
        {
            if (chapterId.HasValue)
            {
                var chapterName = await db.Chapters
                    .AsNoTracking()
                    .Where(x => x.Id == chapterId.Value)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(chapterName)
                    && chapterName.Contains("反常积分", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var combined = $"{problemText} {studentSolutionText}".ToLowerInvariant();
            return combined.Contains("∞")
                || combined.Contains("无穷")
                || combined.Contains("反常积分")
                || combined.Contains("∫_1^∞")
                || combined.Contains("∫1^∞");
        }

        private static string NormalizeLabel(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return input.Trim()
                .Replace("（", "(")
                .Replace("）", ")")
                .Replace(" ", string.Empty);
        }

        private static void AddCode(string code, HashSet<string> seen, List<string> output)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            if (seen.Add(code))
            {
                output.Add(code);
            }
        }
    }
}
