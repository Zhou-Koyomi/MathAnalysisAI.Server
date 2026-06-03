using System.Text.RegularExpressions;
using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Knowledge;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Knowledge
{
    public class KnowledgeRetrievalService : IKnowledgeRetrievalService
    {
        private static readonly string[] MathTerms =
        {
            "反常积分", "收敛", "发散", "比较判别法", "判别法", "p积分", "极限", "级数", "函数列", "一致收敛", "幂级数", "泰勒", "导数", "积分", "无穷"
        };

        private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "因为", "所以", "请", "请问", "判断", "这个", "那个", "进行", "并且", "然后", "我们", "你", "我", "他", "她", "它", "是否", "怎么", "如何", "求", "计算", "证明"
        };

        private readonly ApplicationDbContext _db;

        public KnowledgeRetrievalService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<KnowledgeChunkContextDto>> RetrieveAsync(
            KnowledgeRetrievalRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.CourseId <= 0)
            {
                return Array.Empty<KnowledgeChunkContextDto>();
            }

            var topK = Math.Clamp(request.TopK <= 0 ? 3 : request.TopK, 1, 8);
            var keywords = BuildKeywords(request.ProblemText, request.StudentSolutionText);

            var candidateQuery = _db.MaterialChunks
                .AsNoTracking()
                .Where(c => c.CourseId == request.CourseId)
                .Where(c => !string.IsNullOrWhiteSpace(c.ContentPreview))
                .Where(c => c.CourseMaterial != null && c.CourseMaterial.ParseStatus == "success");

            if (request.ChapterId.HasValue)
            {
                var chapterId = request.ChapterId.Value;
                candidateQuery = candidateQuery.Where(c => c.ChapterId == chapterId);
            }

            var candidates = await candidateQuery
                .OrderByDescending(c => c.CourseMaterial!.UploadedAt)
                .Take(600)
                .Select(c => new ChunkCandidate
                {
                    ChunkId = c.Id,
                    MaterialId = c.CourseMaterialId,
                    CourseId = c.CourseId,
                    ChapterId = c.ChapterId,
                    Title = c.CourseMaterial!.Title,
                    MaterialKind = c.CourseMaterial.MaterialKind,
                    UploadedAt = c.CourseMaterial.UploadedAt,
                    SectionTitle = c.SectionTitle,
                    SectionPath = c.SectionPath,
                    PageStart = c.PageStart,
                    PageEnd = c.PageEnd,
                    ChunkType = c.ChunkType,
                    ContentPreview = c.ContentPreview,
                    IsVerified = c.IsVerified
                })
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return Array.Empty<KnowledgeChunkContextDto>();
            }

            var candidateChunkIds = candidates.Select(c => c.ChunkId).ToList();
            var mappedKnowledgePoints = await ResolveRequestedKnowledgePointsAsync(
                request.CourseId,
                request.NormalizedKnowledgePointCodes,
                cancellationToken);

            var candidateLinks = await _db.MaterialChunkKnowledgePoints
                .AsNoTracking()
                .Where(x => candidateChunkIds.Contains(x.MaterialChunkId))
                .Select(x => new
                {
                    x.MaterialChunkId,
                    x.KnowledgePointId,
                    x.IsPrimary,
                    x.Confidence,
                    KnowledgePointCode = x.KnowledgePoint != null ? x.KnowledgePoint.Code : null,
                    KnowledgePointName = x.KnowledgePoint != null ? x.KnowledgePoint.Name : null
                })
                .ToListAsync(cancellationToken);

            var linksByChunk = candidateLinks
                .GroupBy(x => x.MaterialChunkId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = new List<ScoredChunk>(candidates.Count);
            foreach (var candidate in candidates)
            {
                var score = 0m;

                if (request.ChapterId.HasValue && candidate.ChapterId == request.ChapterId.Value)
                {
                    score += 2m;
                }

                if (keywords.Count > 0)
                {
                    score += ScoreTitleSectionAndPath(candidate, keywords);
                    score += ScoreContentPreview(candidate.ContentPreview, keywords);
                }

                score += ScoreChunkType(candidate.ChunkType);

                if (candidate.IsVerified)
                {
                    score += 1m;
                }

                var matchedKnowledgePointTexts = new List<string>();
                if (linksByChunk.TryGetValue(candidate.ChunkId, out var links))
                {
                    foreach (var link in links)
                    {
                        if (!mappedKnowledgePoints.ContainsKey(link.KnowledgePointId))
                        {
                            continue;
                        }

                        score += 2m;
                        if (link.IsPrimary)
                        {
                            score += 1m;
                        }

                        var confidenceBonus = Math.Min(link.Confidence, 1m) * 0.5m;
                        score += confidenceBonus;

                        var shortInfo = !string.IsNullOrWhiteSpace(link.KnowledgePointCode)
                            ? link.KnowledgePointCode!
                            : (!string.IsNullOrWhiteSpace(link.KnowledgePointName) ? link.KnowledgePointName! : string.Empty);

                        if (!string.IsNullOrWhiteSpace(shortInfo)
                            && !matchedKnowledgePointTexts.Any(x => string.Equals(x, shortInfo, StringComparison.OrdinalIgnoreCase)))
                        {
                            matchedKnowledgePointTexts.Add(shortInfo);
                        }
                    }
                }

                if (score <= 0m)
                {
                    continue;
                }

                results.Add(new ScoredChunk
                {
                    Candidate = candidate,
                    Score = Math.Round(score, 3),
                    MatchedKnowledgePoints = matchedKnowledgePointTexts
                });
            }

            var ordered = results
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Candidate.IsVerified)
                .ThenByDescending(x => x.Candidate.UploadedAt)
                .Take(topK)
                .Select(x => new KnowledgeChunkContextDto
                {
                    ChunkId = x.Candidate.ChunkId,
                    MaterialId = x.Candidate.MaterialId,
                    Title = x.Candidate.Title,
                    MaterialKind = x.Candidate.MaterialKind,
                    SectionTitle = x.Candidate.SectionTitle,
                    SectionPath = x.Candidate.SectionPath,
                    PageStart = x.Candidate.PageStart,
                    PageEnd = x.Candidate.PageEnd,
                    ChunkType = x.Candidate.ChunkType,
                    ContentPreview = Truncate(x.Candidate.ContentPreview, 500),
                    MatchedKnowledgePoints = x.MatchedKnowledgePoints,
                    Score = x.Score,
                    SourceLabel = "sql_keyword"
                })
                .ToList();

            return ordered;
        }

        private async Task<Dictionary<int, string>> ResolveRequestedKnowledgePointsAsync(
            int courseId,
            IReadOnlyList<string>? normalizedKnowledgePointCodes,
            CancellationToken cancellationToken)
        {
            if (normalizedKnowledgePointCodes == null || normalizedKnowledgePointCodes.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var codes = normalizedKnowledgePointCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var points = await _db.KnowledgePoints
                .AsNoTracking()
                .Where(x => x.CourseId == courseId && x.Code != null && codes.Contains(x.Code))
                .Select(x => new { x.Id, x.Code })
                .ToListAsync(cancellationToken);

            return points
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .ToDictionary(x => x.Id, x => x.Code!);
        }

        private static decimal ScoreTitleSectionAndPath(ChunkCandidate candidate, IReadOnlyList<string> keywords)
        {
            decimal score = 0m;
            foreach (var keyword in keywords)
            {
                if (Contains(candidate.Title, keyword)) score += 1m;
                if (Contains(candidate.SectionTitle, keyword)) score += 1m;
                if (Contains(candidate.SectionPath, keyword)) score += 1m;
            }
            return score;
        }

        private static decimal ScoreContentPreview(string? preview, IReadOnlyList<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(preview) || keywords.Count == 0)
            {
                return 0m;
            }

            decimal score = 0m;
            foreach (var keyword in keywords)
            {
                if (Contains(preview, keyword))
                {
                    score += 1.5m;
                }
            }
            return score;
        }

        private static decimal ScoreChunkType(string? chunkType)
        {
            var normalized = (chunkType ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "definition" => 1.5m,
                "theorem" => 1.5m,
                "method" => 1.5m,
                "example" => 1m,
                _ => 0m
            };
        }

        private static List<string> BuildKeywords(string? problemText, string? studentSolutionText)
        {
            var combined = (problemText ?? string.Empty) + " " + (studentSolutionText ?? string.Empty);
            if (string.IsNullOrWhiteSpace(combined))
            {
                return new List<string>();
            }

            var expanded = combined
                .Replace("\\int", " 积分 ", StringComparison.OrdinalIgnoreCase)
                .Replace("∫", " 积分 ", StringComparison.OrdinalIgnoreCase)
                .Replace("\\infty", " 无穷 ", StringComparison.OrdinalIgnoreCase)
                .Replace("∞", " 无穷 ", StringComparison.OrdinalIgnoreCase)
                .Replace("\\sum", " 级数 ", StringComparison.OrdinalIgnoreCase)
                .Replace("\\lim", " 极限 ", StringComparison.OrdinalIgnoreCase);

            var words = new List<string>();
            foreach (var term in MathTerms)
            {
                if (expanded.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    words.Add(term);
                }
            }

            var tokens = Regex.Split(expanded, "[^\\p{L}\\p{Nd}_]+")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => x.Length >= 2)
                .Where(x => !Stopwords.Contains(x));

            words.AddRange(tokens);

            var result = words
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            return result;
        }

        private static bool Contains(string? text, string keyword)
        {
            return !string.IsNullOrWhiteSpace(text)
                   && !string.IsNullOrWhiteSpace(keyword)
                   && text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private sealed class ChunkCandidate
        {
            public int ChunkId { get; set; }
            public int MaterialId { get; set; }
            public int CourseId { get; set; }
            public int? ChapterId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MaterialKind { get; set; } = string.Empty;
            public DateTime UploadedAt { get; set; }
            public string? SectionTitle { get; set; }
            public string? SectionPath { get; set; }
            public int? PageStart { get; set; }
            public int? PageEnd { get; set; }
            public string ChunkType { get; set; } = "unknown";
            public string ContentPreview { get; set; } = string.Empty;
            public bool IsVerified { get; set; }
        }

        private sealed class ScoredChunk
        {
            public ChunkCandidate Candidate { get; set; } = new();
            public decimal Score { get; set; }
            public List<string> MatchedKnowledgePoints { get; set; } = new();
        }
    }
}
