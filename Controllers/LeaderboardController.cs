using MathAnalysisAI.Server.Services.Ranking;
using Microsoft.AspNetCore.Mvc;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardService _leaderboardService;

        public LeaderboardController(LeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicLeaderboard(
            [FromQuery] int courseId = 200,
            [FromQuery] int take = 10,
            CancellationToken cancellationToken = default)
        {
            var rows = await _leaderboardService.GetPublicLeaderboardAsync(courseId, take, cancellationToken);

            var result = rows.Select(x => new
            {
                rank = x.Rank,
                username = x.Username,
                attemptCount = x.AttemptCount,
                correctCount = x.CorrectCount,
                wrongCount = x.WrongCount,
                accuracyRate = x.AccuracyRate,
                rankingScore = x.RankingScore
            });

            return Ok(result);
        }
    }
}
