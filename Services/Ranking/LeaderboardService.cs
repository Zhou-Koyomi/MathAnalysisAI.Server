using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Leaderboard;
using MathAnalysisAI.Server.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Ranking
{
    public class LeaderboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly PermissionService _permissionService;

        public LeaderboardService(ApplicationDbContext db, PermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async Task<List<LeaderboardEntryDto>> GetPublicLeaderboardAsync(
            int courseId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (courseId <= 0)
            {
                return new List<LeaderboardEntryDto>();
            }

            limit = NormalizeLimit(limit);

            var rows = await _db.UserCourseStats
                .AsNoTracking()
                .Where(x => x.CourseId == courseId)
                .OrderByDescending(x => x.RankingScore)
                .ThenByDescending(x => x.AccuracyRate)
                .ThenByDescending(x => x.AttemptCount)
                .Take(limit)
                .Select(x => new
                {
                    x.UserId,
                    Username = x.User != null ? x.User.Username : string.Empty,
                    x.CourseId,
                    CourseName = x.Course != null ? x.Course.Name : string.Empty,
                    x.AttemptCount,
                    x.CorrectCount,
                    x.WrongCount,
                    x.AccuracyRate,
                    x.RankingScore
                })
                .ToListAsync(cancellationToken);

            return rows.Select((x, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = x.UserId,
                Username = x.Username,
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                AttemptCount = x.AttemptCount,
                CorrectCount = x.CorrectCount,
                WrongCount = x.WrongCount,
                AccuracyRate = x.AccuracyRate,
                RankingScore = x.RankingScore
            }).ToList();
        }

        public async Task<List<TeacherLeaderboardEntryDto>> GetTeacherLeaderboardAsync(
            int viewerUserId,
            int courseId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (viewerUserId <= 0 || courseId <= 0)
            {
                return new List<TeacherLeaderboardEntryDto>();
            }

            limit = NormalizeLimit(limit);

            var canViewRealNames = await _permissionService
                .CanViewCourseLeaderboardWithRealNamesAsync(viewerUserId, courseId, cancellationToken);

            var rows = await _db.UserCourseStats
                .AsNoTracking()
                .Where(x => x.CourseId == courseId)
                .OrderByDescending(x => x.RankingScore)
                .ThenByDescending(x => x.AccuracyRate)
                .ThenByDescending(x => x.AttemptCount)
                .Take(limit)
                .Select(x => new
                {
                    x.UserId,
                    Username = x.User != null ? x.User.Username : string.Empty,
                    RealName = x.User != null ? x.User.RealName : null,
                    StudentNumber = x.User != null ? x.User.StudentNumber : null,
                    ClassName = x.User != null ? x.User.ClassName : null,
                    x.CourseId,
                    CourseName = x.Course != null ? x.Course.Name : string.Empty,
                    x.AttemptCount,
                    x.CorrectCount,
                    x.WrongCount,
                    x.AccuracyRate,
                    x.RankingScore
                })
                .ToListAsync(cancellationToken);

            return rows.Select((x, index) => new TeacherLeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = x.UserId,
                Username = x.Username,
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                AttemptCount = x.AttemptCount,
                CorrectCount = x.CorrectCount,
                WrongCount = x.WrongCount,
                AccuracyRate = x.AccuracyRate,
                RankingScore = x.RankingScore,
                RealName = canViewRealNames ? x.RealName : null,
                StudentNumber = canViewRealNames ? x.StudentNumber : null,
                ClassName = canViewRealNames ? x.ClassName : null
            }).ToList();
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 20;
            }

            return Math.Min(limit, 200);
        }
    }
}
