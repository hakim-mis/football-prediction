using FootballPredictionGame.Models;

namespace FootballPredictionGame.ViewModels;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
    public int TotalScore { get; set; }
    public int ExactPredictionCount { get; set; }
    public int? Rank { get; set; }
    public string RankText => Rank.HasValue ? $"#{Rank.Value}" : "No rank";

    public List<LeaderboardUserViewModel> TopUsers { get; set; } = new();
    public List<Fixture> TodayFixtures { get; set; } = new();
    public List<Fixture> UpcomingFixtures { get; set; } = new();
    public List<Fixture> PredictionFixtures { get; set; } = new();
    public List<Prediction> RecentPredictions { get; set; } = new();
    public List<SegmentPointViewModel> SegmentPoints { get; set; } = new();
    public Dictionary<int, Prediction> UserPredictionLookup { get; set; } = new();
}
