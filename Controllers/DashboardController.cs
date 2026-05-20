using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Controllers;

[Authorize(Roles = "User")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!currentUser.IsActive)
        {
            await _signInManager.SignOutAsync();
            TempData["Error"] = "Your account is inactive. Please contact the administrator.";
            return RedirectToAction("Login", "Account");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ForceChangePassword", "Account");
        }

        var orderedUsers = await GetRankedUsersAsync();
        var currentRank = orderedUsers.FirstOrDefault(x => x.UserId == currentUser.Id)?.Rank;

        var now = DateTime.Now;
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);

        var todayFixtures = await _context.Fixtures
            .Where(x => x.IsPublished && x.MatchDateTime >= todayStart && x.MatchDateTime < todayEnd)
            .OrderBy(x => x.MatchDateTime)
            .ToListAsync();

        var upcomingFixtures = await _context.Fixtures
            .Where(x => x.IsPublished && x.MatchDateTime >= todayEnd && !x.IsProcessed)
            .OrderBy(x => x.MatchDateTime)
            .Take(10)
            .ToListAsync();

        var predictionFixtures = await _context.Fixtures
            .Where(x => x.IsPublished && x.MatchDateTime >= todayStart.AddDays(-1) && x.MatchDateTime <= now.AddDays(30))
            .OrderBy(x => x.Stage)
            .ThenBy(x => x.MatchDateTime)
            .Take(30)
            .ToListAsync();

        var fixtureIds = predictionFixtures.Select(x => x.Id).ToList();
        var predictionLookup = await _context.Predictions
            .Where(x => x.UserId == userId && fixtureIds.Contains(x.FixtureId))
            .ToDictionaryAsync(x => x.FixtureId, x => x);

        var recentPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        var processedUserPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x => x.UserId == userId && x.IsProcessed)
            .ToListAsync();

        var segmentPoints = processedUserPredictions
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

        var model = new DashboardViewModel
        {
            FullName = currentUser.FullName,
            ProfilePhotoPath = currentUser.ProfilePhotoPath,
            TotalScore = currentUser.TotalScore,
            ExactPredictionCount = currentUser.ExactPredictionCount,
            Rank = currentRank,
            TopUsers = orderedUsers.Take(10).ToList(),
            TodayFixtures = todayFixtures,
            UpcomingFixtures = upcomingFixtures,
            PredictionFixtures = predictionFixtures,
            RecentPredictions = recentPredictions,
            SegmentPoints = segmentPoints,
            UserPredictionLookup = predictionLookup
        };

        return View(model);
    }

    private static string GetStageName(FixtureStage stage) => stage switch
    {
        FixtureStage.GroupA => "Group A",
        FixtureStage.GroupB => "Group B",
        FixtureStage.GroupC => "Group C",
        FixtureStage.GroupD => "Group D",
        FixtureStage.QuarterFinal => "Quarter Final",
        FixtureStage.SemiFinal => "Semi Final",
        FixtureStage.Final => "Final",
        _ => stage.ToString()
    };

    private async Task<List<LeaderboardUserViewModel>> GetRankedUsersAsync()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");
        return LeaderboardRankingHelper.Build(players);
    }

}
