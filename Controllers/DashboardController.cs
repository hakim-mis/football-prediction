using FootballPredictionGame.Data;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.Models;
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

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index(
        string? status = null,
        string? stage = null,
        DateTime? matchDate = null,
        string? quickFilter = null,
        int? fixtureId = null,
        string groupMode = "date-status")
    {
        var model = await BuildDashboardModelAsync(
            status,
            stage,
            matchDate,
            quickFilter,
            fixtureId,
            groupMode
        );

        if (model == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }

    public async Task<IActionResult> PredictionScore(
        string? status = null,
        string? stage = null,
        DateTime? matchDate = null,
        string? quickFilter = null,
        int? fixtureId = null,
        string groupMode = "date-status")
    {
        var model = await BuildDashboardModelAsync(
            status,
            stage,
            matchDate,
            quickFilter,
            fixtureId,
            groupMode
        );

        if (model == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PredictionDetail(int fixtureId)
    {
        var currentUser = await GetCurrentValidUserAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var fixture = await _context.Fixtures
            .FirstOrDefaultAsync(x => x.Id == fixtureId && x.IsPublished);

        if (fixture == null)
        {
            return NotFound();
        }

        if (fixture.Status != MatchStatus.Live && fixture.Status != MatchStatus.Finished)
        {
            TempData["Error"] = "Prediction details are available only for live or finished fixtures.";
            return RedirectToAction(nameof(Index));
        }

        var predictions = await _context.Predictions
            .Include(x => x.User)
            .Include(x => x.Fixture)
            .Where(x => x.FixtureId == fixtureId)
            .OrderByDescending(x => x.EarnedPoint)
            .ThenByDescending(x => x.User.TotalScore)
            .ThenBy(x => x.User.CreatedAt)
            .ToListAsync();

        var rankedUsers = await GetRankedUsersAsync();

        var rankLookup = rankedUsers
            .ToDictionary(x => x.UserId, x => x.RankText);

        var model = new FixturePredictionDetailViewModel
        {
            FixtureId = fixture.Id,
            StageName = GetStageName(fixture.Stage),
            StatusName = fixture.Status.ToString(),
            MatchDateTime = fixture.MatchDateTime,

            TeamOneName = fixture.TeamOneName,
            TeamOneFlagPath = fixture.TeamOneFlagPath ?? "/img/default-flag.svg",

            TeamTwoName = fixture.TeamTwoName,
            TeamTwoFlagPath = fixture.TeamTwoFlagPath ?? "/img/default-flag.svg",

            TeamOneActualGoal = fixture.TeamOneActualGoal,
            TeamTwoActualGoal = fixture.TeamTwoActualGoal,

            Predictions = predictions.Select(x => new FixturePredictionUserDetailViewModel
            {
                UserId = x.UserId,

                UserName = x.User.FullName,
                PhotoPath = x.User.ProfilePhotoPath ?? "/img/default-avatar.svg",
                RankText = rankLookup.ContainsKey(x.UserId) ? rankLookup[x.UserId] : "No rank",

                Designation = x.User.Designation,
                Department = x.User.Department,

                TeamOnePredictedGoal = x.TeamOnePredictedGoal,
                TeamTwoPredictedGoal = x.TeamTwoPredictedGoal,

                EarnedPoint = x.EarnedPoint,
                TotalScore = x.User.TotalScore
            }).ToList()
        };

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> UserPredictionDetail(string userId)
    {
        var currentUser = await GetCurrentValidUserAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }

        var targetUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (targetUser == null)
        {
            return NotFound();
        }

        var rankedUsers = await GetRankedUsersAsync();

        var rankText = rankedUsers
            .FirstOrDefault(x => x.UserId == targetUser.Id)?
            .RankText ?? "No rank";

        var finishedAndLiveFixtures = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                (x.Status == MatchStatus.Live || x.Status == MatchStatus.Finished))
            .OrderByDescending(x => x.MatchDateTime)
            .ToListAsync();

        var fixtureIds = finishedAndLiveFixtures
            .Select(x => x.Id)
            .ToList();

        var userPredictions = await _context.Predictions
            .Where(x => x.UserId == userId && fixtureIds.Contains(x.FixtureId))
            .ToListAsync();

        var predictionLookup = userPredictions
            .ToDictionary(x => x.FixtureId, x => x);

        var model = new UserPredictionHistoryViewModel
        {
            UserId = targetUser.Id,
            FullName = targetUser.FullName,
            Designation = targetUser.Designation,
            Department = targetUser.Department,
            PhotoPath = targetUser.ProfilePhotoPath ?? "/img/default-avatar.svg",

            RankText = rankText,
            TotalScore = targetUser.TotalScore,
            ExactPredictionCount = targetUser.ExactPredictionCount,

            Items = finishedAndLiveFixtures.Select(fixture =>
            {
                predictionLookup.TryGetValue(fixture.Id, out var prediction);

                return new UserPredictionHistoryItemViewModel
                {
                    FixtureId = fixture.Id,

                    StageName = GetStageName(fixture.Stage),
                    Status = fixture.Status,
                    MatchDateTime = fixture.MatchDateTime,

                    TeamOneName = fixture.TeamOneName,
                    TeamOneFlagPath = fixture.TeamOneFlagPath ?? "/img/default-flag.svg",

                    TeamTwoName = fixture.TeamTwoName,
                    TeamTwoFlagPath = fixture.TeamTwoFlagPath ?? "/img/default-flag.svg",

                    TeamOneActualGoal = fixture.TeamOneActualGoal,
                    TeamTwoActualGoal = fixture.TeamTwoActualGoal,

                    HasPrediction = prediction != null,

                    TeamOnePredictedGoal = prediction?.TeamOnePredictedGoal,
                    TeamTwoPredictedGoal = prediction?.TeamTwoPredictedGoal,

                    EarnedPoint = prediction?.EarnedPoint ?? 0
                };
            }).ToList()
        };

        return View(model);
    }

    private async Task<DashboardViewModel?> BuildDashboardModelAsync(
        string? status,
        string? stage,
        DateTime? matchDate,
        string? quickFilter,
        int? fixtureId,
        string groupMode)
    {
        var currentUser = await GetCurrentValidUserAsync();

        if (currentUser == null)
        {
            return null;
        }

        var userId = currentUser.Id;

        var rankedUsers = await GetRankedUsersAsync();
        var currentRank = rankedUsers.FirstOrDefault(x => x.UserId == currentUser.Id)?.Rank;

        var now = DateTime.Now;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var dayAfterTomorrow = today.AddDays(2);

        var baseFixtureQuery = _context.Fixtures
            .Where(x => x.IsPublished);

        /*
            IMPORTANT:
            For the new dashboard and PredictionScore page,
            we load ALL published fixtures by default.

            Filtering will be handled by JavaScript/AJAX-style buttons in the view.
            So we do not apply old server-side status/date/group filters here.
        */

        var allPublishedFixtures = await baseFixtureQuery
            .OrderBy(x => x.MatchDateTime)
            .ThenBy(x => x.Stage)
            .ToListAsync();

        var allFixtureIds = allPublishedFixtures
            .Select(x => x.Id)
            .ToList();

        var allUserPredictions = await _context.Predictions
            .Where(x => x.UserId == userId && allFixtureIds.Contains(x.FixtureId))
            .ToListAsync();

        var predictedFixtureIds = allUserPredictions
            .Select(x => x.FixtureId)
            .ToHashSet();

        var pendingPredictionCount = allPublishedFixtures.Count(x =>
            x.Status == MatchStatus.Upcoming &&
            !x.IsProcessed &&
            x.MatchDateTime > now &&
            !predictedFixtureIds.Contains(x.Id));

        var predictedCount = allPublishedFixtures.Count(x =>
            predictedFixtureIds.Contains(x.Id));

        var notParticipateCount = allPublishedFixtures.Count(x =>
            (x.Status == MatchStatus.Live ||
             x.Status == MatchStatus.Finished ||
             x.MatchDateTime <= now) &&
            !predictedFixtureIds.Contains(x.Id));

        var finishedFixtureCount = allPublishedFixtures.Count(x =>
            x.Status == MatchStatus.Finished);

        var liveFixtureCount = allPublishedFixtures.Count(x =>
            x.Status == MatchStatus.Live);

        var upcomingFixtureCount = allPublishedFixtures.Count(x =>
            x.Status == MatchStatus.Upcoming);

        var todaysMatchCount = allPublishedFixtures.Count(x =>
            x.MatchDateTime >= today &&
            x.MatchDateTime < tomorrow);

        var tomorrowMatchCount = allPublishedFixtures.Count(x =>
            x.MatchDateTime >= tomorrow &&
            x.MatchDateTime < dayAfterTomorrow);

        /*
            PredictionFixtures:
            - Normally all published fixtures.
            - If fixtureId is provided, only show that one fixture.
              This keeps your previous Predict button redirect support if needed.
        */

        var predictionFixturesQuery = baseFixtureQuery.AsQueryable();

        if (fixtureId.HasValue)
        {
            predictionFixturesQuery = predictionFixturesQuery
                .Where(x => x.Id == fixtureId.Value);
        }

        var predictionFixtures = await predictionFixturesQuery
            .OrderBy(x => x.MatchDateTime)
            .ThenBy(x => x.Stage)
            .ToListAsync();

        var visibleFixtureIds = predictionFixtures
            .Select(x => x.Id)
            .ToList();

        var predictionLookup = await _context.Predictions
            .Where(x => x.UserId == userId && visibleFixtureIds.Contains(x.FixtureId))
            .ToDictionaryAsync(x => x.FixtureId, x => x);

        var todayFixtures = allPublishedFixtures
            .Where(x => x.MatchDateTime >= today && x.MatchDateTime < tomorrow)
            .OrderBy(x => x.MatchDateTime)
            .ToList();

        var upcomingFixtures = allPublishedFixtures
            .Where(x => x.Status == MatchStatus.Upcoming && x.MatchDateTime >= now)
            .OrderBy(x => x.MatchDateTime)
            .Take(10)
            .ToList();

        var recentPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var processedUserPredictions = recentPredictions
            .Where(x => x.IsProcessed)
            .ToList();

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

        var totalPredictionCount = recentPredictions.Count;
        var finishedPredictionCount = recentPredictions.Count(x => x.IsProcessed);

        var model = new DashboardViewModel
        {
            FullName = currentUser.FullName,
            ProfilePhotoPath = currentUser.ProfilePhotoPath,

            FixtureId = fixtureId,

            TotalScore = currentUser.TotalScore,
            ExactPredictionCount = currentUser.ExactPredictionCount,

            Rank = currentRank,
            TopScore = rankedUsers.Any() ? rankedUsers.Max(x => x.TotalScore) : 0,

            TopUsers = rankedUsers.Take(10).ToList(),

            TodayFixtures = todayFixtures,
            UpcomingFixtures = upcomingFixtures,
            PredictionFixtures = predictionFixtures,
            RecentPredictions = recentPredictions,
            SegmentPoints = segmentPoints,
            UserPredictionLookup = predictionLookup,

            /*
                We keep these properties for compatibility,
                but the new dashboard view does not use server-side filtering.
            */
            FilterStatus = status,
            FilterStage = stage,
            FilterDate = matchDate,
            QuickFilter = quickFilter,
            GroupMode = string.IsNullOrWhiteSpace(groupMode) ? "date-status" : groupMode,

            PendingPredictionCount = pendingPredictionCount,
            PredictedCount = predictedCount,
            NotParticipateCount = notParticipateCount,
            FinishedFixtureCount = finishedFixtureCount,
            TodaysMatchCount = todaysMatchCount,
            TomorrowMatchCount = tomorrowMatchCount,

            LiveFixtureCount = liveFixtureCount,
            UpcomingFixtureCount = upcomingFixtureCount,

            TotalPredictionCount = totalPredictionCount,
            FinishedPredictionCount = finishedPredictionCount,

            Banners = GetDashboardBanners()
        };

        return model;
    }

    private async Task<ApplicationUser?> GetCurrentValidUserAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var currentUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (currentUser == null)
        {
            return null;
        }

        if (!currentUser.IsActive)
        {
            await _signInManager.SignOutAsync();
            TempData["Error"] = "Your account is inactive. Please contact the administrator.";
            return null;
        }

        if (currentUser.MustChangePassword)
        {
            TempData["Error"] = "Please change your default password first.";
            return null;
        }

        return currentUser;
    }

    private List<DashboardBannerViewModel> GetDashboardBanners()
    {
        return new List<DashboardBannerViewModel>
        {
            new DashboardBannerViewModel
            {
                Title = "Transtec 360° Football Prediction",
                Subtitle = "Predict fixtures, track points, and climb the leaderboard.",
                ImageUrl = "/img/banner1.png",
                ButtonText = "More Info",
                RedirectUrl = "https://transcomdigital.com/"
            },
            new DashboardBannerViewModel
            {
                Title = "World Cup Prediction Challenge",
                Subtitle = "Follow live, upcoming, and finished fixtures with smart prediction tracking.",
                ImageUrl = "/img/banner2.png",
                ButtonText = "Explore",
                RedirectUrl = "https://transcomdigital.com/"
            },
            new DashboardBannerViewModel
            {
                Title = "Compete with Colleagues",
                Subtitle = "Submit predictions, earn points, and win bragging rights.",
                ImageUrl = "/img/banner3.png",
                ButtonText = "Get More",
                RedirectUrl = "https://www.transteclighting.com/"
            }
        };
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

    [HttpGet]
    public IActionResult Rules()
    {
        return View();
    }
}