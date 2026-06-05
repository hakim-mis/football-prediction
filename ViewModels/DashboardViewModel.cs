using FootballPredictionGame.Models;

namespace FootballPredictionGame.ViewModels;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public int TotalScore { get; set; }
    public int ExactPredictionCount { get; set; }
    public int? Rank { get; set; }
    public string RankText => Rank.HasValue ? $"#{Rank.Value}" : "No rank";

    public string? FilterStatus { get; set; }
    public string? FilterStage { get; set; }
    public DateTime? FilterDate { get; set; }
    public string GroupMode { get; set; } = "date-status";

    public int LiveFixtureCount { get; set; }
    public int UpcomingFixtureCount { get; set; }
    public int FinishedFixtureCount { get; set; }

    public int TotalPredictionCount { get; set; }
    public int PendingPredictionCount { get; set; }
    public int FinishedPredictionCount { get; set; }
    public int? FixtureId { get; set; }
    public string? QuickFilter { get; set; }

    public int TopScore { get; set; }

    public int PredictedCount { get; set; }
    public int NotParticipateCount { get; set; }
    public int TodaysMatchCount { get; set; }
    public int TomorrowMatchCount { get; set; }

    public List<DashboardBannerViewModel> Banners { get; set; } = new();


    public List<LeaderboardUserViewModel> TopUsers { get; set; } = new();
    public List<Fixture> TodayFixtures { get; set; } = new();
    public List<Fixture> UpcomingFixtures { get; set; } = new();
    public List<Fixture> PredictionFixtures { get; set; } = new();
    public List<Prediction> RecentPredictions { get; set; } = new();
    public List<SegmentPointViewModel> SegmentPoints { get; set; } = new();
    public Dictionary<int, Prediction> UserPredictionLookup { get; set; } = new();
}
