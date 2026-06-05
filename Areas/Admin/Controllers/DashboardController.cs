using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var topUsers = LeaderboardRankingHelper.Build(
            players,
            take: 10,
            scoredOnly: true
        );

        var totalUsers = players.Count;
        var activeUsers = players.Count(x => x.IsActive);
        var pendingUsers = players.Count(x => !x.IsActive);
        var emailVerifiedUsers = players.Count(x => x.EmailConfirmed);
        var emailNotVerifiedUsers = players.Count(x => !x.EmailConfirmed);

        var totalFixtures = await _context.Fixtures.CountAsync();
        var publishedFixtures = await _context.Fixtures.CountAsync(x => x.IsPublished);
        var unpublishedFixtures = await _context.Fixtures.CountAsync(x => !x.IsPublished);

        var upcomingFixtures = await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Upcoming);
        var liveFixtures = await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Live);
        var finishedFixtures = await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Finished);

        var processedResults = await _context.Fixtures.CountAsync(x => x.IsProcessed);

        var pendingProcessResults = await _context.Fixtures.CountAsync(x =>
            !x.IsProcessed &&
            x.Status == MatchStatus.Finished &&
            x.TeamOneActualGoal.HasValue &&
            x.TeamTwoActualGoal.HasValue
        );

        var totalPredictions = await _context.Predictions.CountAsync();
        var processedPredictionsCount = await _context.Predictions.CountAsync(x => x.IsProcessed);
        var pendingPredictionsCount = await _context.Predictions.CountAsync(x => !x.IsProcessed);

        var processedPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x => x.IsProcessed)
            .ToListAsync();

        var segmentPoints = processedPredictions
            .GroupBy(x => x.Fixture.Stage)
            .Select(g => new SegmentPointViewModel
            {
                SegmentName = GetStageName(g.Key),
                Points = g.Sum(x => x.EarnedPoint),
                ExactCount = g.Count(x => x.EarnedPoint == 3),
                PredictionCount = g.Count()
            })
            .OrderByDescending(x => x.Points)
            .ToList();

        var loggedInUsers = await _context.UserActiveSessions
    .CountAsync(x => x.IsActive);

        var todayLogins = await _context.UserLoginHistories
            .CountAsync(x =>
                x.IsSuccess &&
                x.LoginAt.Date == DateTime.Today);

        var model = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            PendingUsers = pendingUsers,
            ActiveUsers = activeUsers,
            EmailVerifiedUsers = emailVerifiedUsers,
            EmailNotVerifiedUsers = emailNotVerifiedUsers,

            TotalFixtures = totalFixtures,
            PublishedFixtures = publishedFixtures,
            UnpublishedFixtures = unpublishedFixtures,
            UpcomingFixtures = upcomingFixtures,
            LiveFixtures = liveFixtures,
            FinishedFixtures = finishedFixtures,

            ProcessedResults = processedResults,
            PendingProcessResults = pendingProcessResults,

            TotalPredictions = totalPredictions,
            ProcessedPredictions = processedPredictionsCount,
            PendingPredictions = pendingPredictionsCount,

            TodayLogins = todayLogins,
            LoggedInUsers = loggedInUsers,

            TopUsers = topUsers,
            SegmentPoints = segmentPoints,


            UserStatusCounts = new List<int>
            {
                activeUsers,
                pendingUsers,
                emailNotVerifiedUsers
            },

            FixtureStatusCounts = new List<int>
            {
                upcomingFixtures,
                liveFixtures,
                finishedFixtures
            },

            FixturePublishCounts = new List<int>
            {
                publishedFixtures,
                unpublishedFixtures
            },

            ProcessingCounts = new List<int>
            {
                processedResults,
                pendingProcessResults
            },

            PredictionCounts = new List<int>
            {
                processedPredictionsCount,
                pendingPredictionsCount
            }
        };

        return View(model);
    }

    private static string GetStageName(FixtureStage stage) => stage switch
    {
        FixtureStage.GroupA => "Group A",
        FixtureStage.GroupB => "Group B",
        FixtureStage.GroupC => "Group C",
        FixtureStage.GroupD => "Group D",
        FixtureStage.GroupE => "Group E",
        FixtureStage.GroupF => "Group F",
        FixtureStage.GroupG => "Group G",
        FixtureStage.GroupH => "Group H",
        FixtureStage.GroupI => "Group I",
        FixtureStage.GroupJ => "Group J",
        FixtureStage.GroupK => "Group K",
        FixtureStage.GroupL => "Group L",
        FixtureStage.Roundof32 => "Round of 32",
        FixtureStage.Roundof16 => "Round of 16",
        FixtureStage.QuarterFinal => "Quarter Final",
        FixtureStage.SemiFinal => "Semi Final",
        FixtureStage.Final => "Final",
        _ => stage.ToString()
    };
}