namespace MathAnalysisAI.Server.DTOs.Leaderboard
{
    public class TeacherLeaderboardEntryDto : LeaderboardEntryDto
    {
        public string? RealName { get; set; }
        public string? StudentNumber { get; set; }
        public string? ClassName { get; set; }
    }
}
