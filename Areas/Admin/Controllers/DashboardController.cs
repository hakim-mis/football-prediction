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

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var topUsers = LeaderboardRankingHelper.Build(players, take: 10, scoredOnly: true);

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

        var model = new AdminDashboardViewModel
        {
            TotalUsers = players.Count,
            PendingUsers = players.Count(x => !x.IsActive),
            ActiveUsers = players.Count(x => x.IsActive),
            TotalFixtures = await _context.Fixtures.CountAsync(),
            UpcomingFixtures = await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Upcoming),
            FinishedFixtures = await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Finished),
            ProcessedResults = await _context.Fixtures.CountAsync(x => x.IsProcessed),
            TotalPredictions = await _context.Predictions.CountAsync(),
            TopUsers = topUsers,
            SegmentPoints = segmentPoints,
            FixtureStatusCounts = new List<int>
            {
                await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Upcoming),
                await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Live),
                await _context.Fixtures.CountAsync(x => x.Status == MatchStatus.Finished)
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
