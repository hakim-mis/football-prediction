namespace FootballPredictionGame.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalFixtures { get; set; }
    public int UpcomingFixtures { get; set; }
    public int FinishedFixtures { get; set; }
    public int ProcessedResults { get; set; }
    public int TotalPredictions { get; set; }
    public List<LeaderboardUserViewModel> TopUsers { get; set; } = new();
    public List<SegmentPointViewModel> SegmentPoints { get; set; } = new();
    public List<int> FixtureStatusCounts { get; set; } = new();
}
