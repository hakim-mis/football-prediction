using FootballPredictionGame.Models;

namespace FootballPredictionGame.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int EmailVerifiedUsers { get; set; }
    public int EmailNotVerifiedUsers { get; set; }

    public int TotalFixtures { get; set; }
    public int PublishedFixtures { get; set; }
    public int UnpublishedFixtures { get; set; }
    public int UpcomingFixtures { get; set; }
    public int LiveFixtures { get; set; }
    public int FinishedFixtures { get; set; }

    public int ProcessedResults { get; set; }
    public int PendingProcessResults { get; set; }

    public int TotalPredictions { get; set; }
    public int ProcessedPredictions { get; set; }
    public int PendingPredictions { get; set; }

    public int TodayLogins { get; set; }
    public int LoggedInUsers { get; set; }

    public List<LeaderboardUserViewModel> TopUsers { get; set; } = new();
    public List<SegmentPointViewModel> SegmentPoints { get; set; } = new();

    public List<int> UserStatusCounts { get; set; } = new();
    public List<int> FixtureStatusCounts { get; set; } = new();
    public List<int> FixturePublishCounts { get; set; } = new();
    public List<int> ProcessingCounts { get; set; } = new();
    public List<int> PredictionCounts { get; set; } = new();
}