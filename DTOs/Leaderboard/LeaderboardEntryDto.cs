namespace MathAnalysisAI.Server.DTOs.Leaderboard
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public decimal AccuracyRate { get; set; }
        public decimal RankingScore { get; set; }
    }
}
