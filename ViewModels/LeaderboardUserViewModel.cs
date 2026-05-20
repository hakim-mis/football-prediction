namespace FootballPredictionGame.ViewModels;

public class LeaderboardUserViewModel
{
    public int? Rank { get; set; }
    public string RankText => Rank.HasValue ? $"#{Rank.Value}" : "No rank";
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public int TotalScore { get; set; }
    public int ExactPredictionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
