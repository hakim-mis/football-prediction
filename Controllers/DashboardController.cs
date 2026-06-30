using ClosedXML.Excel;
using FootballPredictionGame.Data;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Controllers;

[Authorize(Roles = "User,Admin")]
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
            .AsNoTracking()
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
            .AsNoTracking()
            .ToListAsync();

        var rankedUsers = await GetRankedUsersAsync();

        var rankLookup = rankedUsers
            .ToDictionary(x => x.UserId, x => x);

        var predictionItems = predictions
            .Select(x =>
            {
                rankLookup.TryGetValue(x.UserId, out var rankInfo);

                return new FixturePredictionUserDetailViewModel
                {
                    UserId = x.UserId,

                    UserName = x.User.FullName,
                    PhotoPath = x.User.ProfilePhotoPath ?? "/img/default-avatar.svg",
                    RankText = rankInfo?.RankText ?? "No rank",

                    Designation = x.User.Designation,
                    Department = x.User.Department,

                    TeamOnePredictedGoal = x.TeamOnePredictedGoal,
                    TeamTwoPredictedGoal = x.TeamTwoPredictedGoal,

                    EarnedPoint = x.EarnedPoint,
                    TotalScore = x.User.TotalScore,
                    ExactPredictionCount = x.User.ExactPredictionCount,
                    WinMatchPredictionCount = rankInfo?.WinMatchPredictionCount ?? 0
                };
            })
            .OrderByDescending(x => x.EarnedPoint)
            .ThenBy(x => RankSortValue(x.RankText))
            .ThenByDescending(x => x.TotalScore)
            //.ThenByDescending(x => x.ExactPredictionCount)
            //.ThenByDescending(x => x.WinMatchPredictionCount)
            .ThenBy(x => x.UserName)
            //.OrderBy(x => RankSortValue(x.RankText))
            //.ThenByDescending(x => x.EarnedPoint)
            //.ThenByDescending(x => x.TotalScore)
            //.ThenByDescending(x => x.ExactPredictionCount)
            //.ThenByDescending(x => x.WinMatchPredictionCount)
            //.ThenBy(x => x.UserName)
            .ToList();

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

            Predictions = predictionItems
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

        var winMatchPredictionCount = rankedUsers
            .FirstOrDefault(x => x.UserId == targetUser.Id)?
            .WinMatchPredictionCount ?? 0;

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
            WinMatchPredictionCount = winMatchPredictionCount,

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
        var currentWinMatchCount = rankedUsers.FirstOrDefault(x => x.UserId == currentUser.Id)?.WinMatchPredictionCount ?? 0;

        var now = DateTime.Now;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var dayAfterTomorrow = today.AddDays(2);

        var baseFixtureQuery = _context.Fixtures
            .Where(x => x.IsPublished);

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
            Designation = currentUser.Designation,
            Department = currentUser.Department,
            FixtureId = fixtureId,

            TotalScore = currentUser.TotalScore,
            ExactPredictionCount = currentUser.ExactPredictionCount,
            WinMatchPredictionCount = currentWinMatchCount,

            Rank = currentRank,
            TopScore = rankedUsers.Any() ? rankedUsers.Max(x => x.TotalScore) : 0,

            TopUsers = rankedUsers.Take(10).ToList(),

            TodayFixtures = todayFixtures,
            UpcomingFixtures = upcomingFixtures,
            PredictionFixtures = predictionFixtures,
            RecentPredictions = recentPredictions,
            SegmentPoints = segmentPoints,
            UserPredictionLookup = predictionLookup,

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

    [HttpGet]
    public async Task<IActionResult> DownloadFixturePredictionsExcel(int fixtureId)
    {
        var currentUser = await GetCurrentValidUserAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var fixture = await _context.Fixtures
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fixtureId && x.IsPublished);

        if (fixture == null)
        {
            return NotFound();
        }

        var predictions = await _context.Predictions
            .Include(x => x.User)
            .Include(x => x.Fixture)
            .Where(x => x.FixtureId == fixtureId)
            .AsNoTracking()
            .ToListAsync();

        var rankedUsers = await GetRankedUsersAsync();

        var rankMap = rankedUsers
            .ToDictionary(x => x.UserId, x => x);

        var actualScore = fixture.TeamOneActualGoal.HasValue && fixture.TeamTwoActualGoal.HasValue
            ? $"{fixture.TeamOneActualGoal} - {fixture.TeamTwoActualGoal}"
            : "Pending";

        var exportRows = predictions
            .Select(prediction =>
            {
                rankMap.TryGetValue(prediction.UserId, out var rankInfo);

                return new
                {
                    RankText = rankInfo?.RankText ?? "No rank",
                    RankSort = RankSortValue(rankInfo?.RankText),
                    UserName = prediction.User.FullName,
                    Designation = prediction.User.Designation ?? "-",
                    Department = prediction.User.Department ?? "-",
                    Prediction = $"{prediction.TeamOnePredictedGoal} - {prediction.TeamTwoPredictedGoal}",
                    Point = prediction.EarnedPoint,
                    TotalScore = prediction.User.TotalScore,
                    Exact = prediction.User.ExactPredictionCount,
                    Win = rankInfo?.WinMatchPredictionCount ?? 0
                };
            })
            .OrderBy(x => x.RankSort)
            .ThenByDescending(x => x.Point)
            .ThenByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.Exact)
            .ThenByDescending(x => x.Win)
            .ThenBy(x => x.UserName)
            .ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Match Predictions");

        worksheet.Cell(1, 1).Value = "Transtec 360° Football Prediction";
        worksheet.Range(1, 1, 1, 14).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(3, 1).Value = "Fixture Info";
        worksheet.Range(3, 1, 3, 14).Merge();
        worksheet.Cell(3, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;

        worksheet.Cell(4, 1).Value = "Segment";
        worksheet.Cell(4, 2).Value = GetStageName(fixture.Stage);

        worksheet.Cell(4, 4).Value = "Status";
        worksheet.Cell(4, 5).Value = fixture.Status.ToString();

        worksheet.Cell(5, 1).Value = "Match Date & Time";
        worksheet.Cell(5, 2).Value = fixture.MatchDateTime.ToString("dd MMM yyyy, hh:mm tt");

        worksheet.Cell(5, 4).Value = "Actual Score";
        worksheet.Cell(5, 5).Value = actualScore;

        worksheet.Cell(6, 1).Value = "Team One";
        worksheet.Cell(6, 2).Value = fixture.TeamOneName;

        worksheet.Cell(6, 4).Value = "Team Two";
        worksheet.Cell(6, 5).Value = fixture.TeamTwoName;

        worksheet.Range(4, 1, 6, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(4, 1, 6, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(4, 1, 6, 1).Style.Font.Bold = true;
        worksheet.Range(4, 4, 6, 4).Style.Font.Bold = true;

        var headerRow = 9;

        worksheet.Cell(headerRow, 1).Value = "SL";
        worksheet.Cell(headerRow, 2).Value = "Rank";
        worksheet.Cell(headerRow, 3).Value = "Player Name";
        worksheet.Cell(headerRow, 4).Value = "Designation";
        worksheet.Cell(headerRow, 5).Value = "Department";
        worksheet.Cell(headerRow, 6).Value = "Prediction";
        worksheet.Cell(headerRow, 7).Value = "Actual Score";
        worksheet.Cell(headerRow, 8).Value = "Earned Point";
        worksheet.Cell(headerRow, 9).Value = "Total Score";
        worksheet.Cell(headerRow, 10).Value = "Exact";
        worksheet.Cell(headerRow, 11).Value = "Win";
        worksheet.Cell(headerRow, 12).Value = "Segment";
        worksheet.Cell(headerRow, 13).Value = "Match Date & Time";
        worksheet.Cell(headerRow, 14).Value = "Status";

        var headerRange = worksheet.Range(headerRow, 1, headerRow, 14);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF6FF");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var row = headerRow + 1;
        var sl = 1;

        if (!exportRows.Any())
        {
            worksheet.Cell(row, 1).Value = "No prediction submitted for this fixture.";
            worksheet.Range(row, 1, row, 14).Merge();
            worksheet.Cell(row, 1).Style.Font.Italic = true;
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        else
        {
            foreach (var item in exportRows)
            {
                worksheet.Cell(row, 1).Value = sl;
                worksheet.Cell(row, 2).Value = item.RankText;
                worksheet.Cell(row, 3).Value = item.UserName;
                worksheet.Cell(row, 4).Value = item.Designation;
                worksheet.Cell(row, 5).Value = item.Department;
                worksheet.Cell(row, 6).Value = item.Prediction;
                worksheet.Cell(row, 7).Value = actualScore;
                worksheet.Cell(row, 8).Value = item.Point;
                worksheet.Cell(row, 9).Value = item.TotalScore;
                worksheet.Cell(row, 10).Value = item.Exact;
                worksheet.Cell(row, 11).Value = item.Win;
                worksheet.Cell(row, 12).Value = GetStageName(fixture.Stage);
                worksheet.Cell(row, 13).Value = fixture.MatchDateTime.ToString("dd MMM yyyy, hh:mm tt");
                worksheet.Cell(row, 14).Value = fixture.Status.ToString();

                if (item.Point == 3)
                {
                    worksheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                    worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                }
                else if (item.Point == 0)
                {
                    worksheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#475569");
                }
                else
                {
                    worksheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#92400E");
                }

                row++;
                sl++;
            }
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        worksheet.Columns().AdjustToContents();

        worksheet.Column(1).Width = 8;
        worksheet.Column(3).Width = 28;
        worksheet.Column(4).Width = 22;
        worksheet.Column(5).Width = 22;
        worksheet.Column(13).Width = 24;

        worksheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var safeTeamOne = string.Join("_", fixture.TeamOneName.Split(Path.GetInvalidFileNameChars()));
        var safeTeamTwo = string.Join("_", fixture.TeamTwoName.Split(Path.GetInvalidFileNameChars()));

        var fileName = $"Match_Predictions_{safeTeamOne}_vs_{safeTeamTwo}_{fixture.MatchDateTime:yyyyMMdd_HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    [HttpGet]
    public async Task<IActionResult> DownloadUserPredictionDetailExcel(string userId)
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

        var targetRankInfo = rankedUsers
            .FirstOrDefault(x => x.UserId == targetUser.Id);

        var rankText = targetRankInfo?.RankText ?? "No rank";
        var winMatchPredictionCount = targetRankInfo?.WinMatchPredictionCount ?? 0;

        var liveAndFinishedFixtures = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                (x.Status == MatchStatus.Live || x.Status == MatchStatus.Finished))
            .OrderByDescending(x => x.MatchDateTime)
            .ToListAsync();

        var fixtureIds = liveAndFinishedFixtures
            .Select(x => x.Id)
            .ToList();

        var userPredictions = await _context.Predictions
            .Where(x => x.UserId == userId && fixtureIds.Contains(x.FixtureId))
            .ToListAsync();

        var predictionLookup = userPredictions
            .ToDictionary(x => x.FixtureId, x => x);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Prediction Detail");

        worksheet.Cell(1, 1).Value = "Transtec 360° Football Prediction";
        worksheet.Range(1, 1, 1, 11).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(2, 1).Value = "User Prediction Detail";
        worksheet.Range(2, 1, 2, 11).Merge();
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 13;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(4, 1).Value = "User Info";
        worksheet.Range(4, 1, 4, 11).Merge();
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;

        worksheet.Cell(5, 1).Value = "Name";
        worksheet.Cell(5, 2).Value = targetUser.FullName;

        worksheet.Cell(5, 4).Value = "Rank";
        worksheet.Cell(5, 5).Value = rankText;

        worksheet.Cell(6, 1).Value = "Designation";
        worksheet.Cell(6, 2).Value = targetUser.Designation ?? "-";

        worksheet.Cell(6, 4).Value = "Department";
        worksheet.Cell(6, 5).Value = targetUser.Department ?? "-";

        worksheet.Cell(7, 1).Value = "Score";
        worksheet.Cell(7, 2).Value = targetUser.TotalScore;

        worksheet.Cell(7, 4).Value = "Exact";
        worksheet.Cell(7, 5).Value = targetUser.ExactPredictionCount;

        worksheet.Cell(8, 1).Value = "Win";
        worksheet.Cell(8, 2).Value = winMatchPredictionCount;

        worksheet.Cell(8, 4).Value = "Generated On";
        worksheet.Cell(8, 5).Value = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

        worksheet.Range(5, 1, 8, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(5, 1, 8, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(5, 1, 8, 1).Style.Font.Bold = true;
        worksheet.Range(5, 4, 8, 4).Style.Font.Bold = true;

        var headerRow = 11;

        worksheet.Cell(headerRow, 1).Value = "SL";
        worksheet.Cell(headerRow, 2).Value = "Match Date";
        worksheet.Cell(headerRow, 3).Value = "Match Time";
        worksheet.Cell(headerRow, 4).Value = "Group";
        worksheet.Cell(headerRow, 5).Value = "Team One";
        worksheet.Cell(headerRow, 6).Value = "Team Two";
        worksheet.Cell(headerRow, 7).Value = "Status";
        worksheet.Cell(headerRow, 8).Value = "Prediction";
        worksheet.Cell(headerRow, 9).Value = "Actual Score";
        worksheet.Cell(headerRow, 10).Value = "Point";
        worksheet.Cell(headerRow, 11).Value = "Participation";

        var headerRange = worksheet.Range(headerRow, 1, headerRow, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF6FF");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var row = headerRow + 1;
        var sl = 1;

        if (!liveAndFinishedFixtures.Any())
        {
            worksheet.Cell(row, 1).Value = "No live or finished fixture found.";
            worksheet.Range(row, 1, row, 11).Merge();
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(row, 1).Style.Font.Italic = true;
        }
        else
        {
            foreach (var fixture in liveAndFinishedFixtures)
            {
                predictionLookup.TryGetValue(fixture.Id, out var prediction);

                var actualScore = fixture.TeamOneActualGoal.HasValue && fixture.TeamTwoActualGoal.HasValue
                    ? $"{fixture.TeamOneActualGoal} - {fixture.TeamTwoActualGoal}"
                    : "Pending";

                var predictionText = prediction != null
                    ? $"{prediction.TeamOnePredictedGoal} - {prediction.TeamTwoPredictedGoal}"
                    : "-";

                var participationText = prediction != null
                    ? "Predicted"
                    : "Not Participated";

                var earnedPoint = prediction?.EarnedPoint ?? 0;

                worksheet.Cell(row, 1).Value = sl;
                worksheet.Cell(row, 2).Value = fixture.MatchDateTime.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 3).Value = fixture.MatchDateTime.ToString("hh:mm tt");
                worksheet.Cell(row, 4).Value = GetStageName(fixture.Stage);
                worksheet.Cell(row, 5).Value = fixture.TeamOneName;
                worksheet.Cell(row, 6).Value = fixture.TeamTwoName;
                worksheet.Cell(row, 7).Value = fixture.Status.ToString();
                worksheet.Cell(row, 8).Value = predictionText;
                worksheet.Cell(row, 9).Value = actualScore;
                worksheet.Cell(row, 10).Value = prediction != null ? earnedPoint : "-";
                worksheet.Cell(row, 11).Value = participationText;

                if (prediction == null)
                {
                    worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.FromHtml("#475569");
                }
                else if (earnedPoint == 3)
                {
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                    worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                }
                else if (earnedPoint == 0)
                {
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#475569");
                }
                else
                {
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#92400E");
                }

                row++;
                sl++;
            }
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        worksheet.Columns().AdjustToContents();

        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 15;
        worksheet.Column(3).Width = 14;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 22;
        worksheet.Column(6).Width = 22;
        worksheet.Column(8).Width = 15;
        worksheet.Column(9).Width = 15;
        worksheet.Column(11).Width = 18;

        worksheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var safeName = string.Join("_", targetUser.FullName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "_");

        var fileName = $"User_Prediction_Detail_{safeName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePredictionAjax(int fixtureId, int teamOneGoal, int teamTwoGoal)
    {
        var currentUser = await GetCurrentValidUserAsync();

        if (currentUser == null)
        {
            return Json(new
            {
                success = false,
                message = "Session expired. Please login again."
            });
        }

        if (teamOneGoal < 0 || teamTwoGoal < 0 || teamOneGoal > 99 || teamTwoGoal > 99)
        {
            return Json(new
            {
                success = false,
                message = "Goal value must be between 0 and 99."
            });
        }

        var fixture = await _context.Fixtures
            .FirstOrDefaultAsync(x => x.Id == fixtureId && x.IsPublished);

        if (fixture == null)
        {
            return Json(new
            {
                success = false,
                message = "Fixture not found."
            });
        }

        if (fixture.Status != MatchStatus.Upcoming || fixture.IsProcessed || DateTime.Now >= fixture.MatchDateTime)
        {
            return Json(new
            {
                success = false,
                message = "Prediction is locked. You can predict only before match starts."
            });
        }

        var existingPrediction = await _context.Predictions
            .FirstOrDefaultAsync(x => x.UserId == currentUser.Id && x.FixtureId == fixtureId);

        var wasNewPrediction = existingPrediction == null;

        if (existingPrediction == null)
        {
            existingPrediction = new Prediction
            {
                UserId = currentUser.Id,
                FixtureId = fixtureId,
                TeamOnePredictedGoal = teamOneGoal,
                TeamTwoPredictedGoal = teamTwoGoal,
                EarnedPoint = 0,
                IsProcessed = false,
                CreatedAt = DateTime.Now
            };

            _context.Predictions.Add(existingPrediction);
        }
        else
        {
            existingPrediction.TeamOnePredictedGoal = teamOneGoal;
            existingPrediction.TeamTwoPredictedGoal = teamTwoGoal;
            existingPrediction.IsProcessed = false;
            existingPrediction.EarnedPoint = 0;
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = wasNewPrediction
                ? "Prediction submitted successfully."
                : "Prediction updated successfully.",

            fixtureId = fixture.Id,
            wasNewPrediction,

            predictionText = $"{teamOneGoal} - {teamTwoGoal}",
            pointText = "Pending",

            predictedCountChange = wasNewPrediction ? 1 : 0,
            noPredictionCountChange = wasNewPrediction ? -1 : 0,

            buttonText = "Update Prediction",
            buttonClass = "btn-primary"
        });
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

    private async Task<List<LeaderboardUserViewModel>> GetRankedUsersAsync()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var userIds = players
            .Select(x => x.Id)
            .ToList();

        var winMatchPredictionCountMap = await BuildWinMatchPredictionCountMapAsync(userIds);

        return LeaderboardRankingHelper.Build(
            users: players,
            take: players.Count,
            scoredOnly: false,
            winMatchPredictionCountMap: winMatchPredictionCountMap);
    }

    private async Task<Dictionary<string, int>> BuildWinMatchPredictionCountMapAsync(List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            return new Dictionary<string, int>();
        }

        var predictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x =>
                userIds.Contains(x.UserId) &&
                x.Fixture.Status == MatchStatus.Finished &&
                x.Fixture.TeamOneActualGoal.HasValue &&
                x.Fixture.TeamTwoActualGoal.HasValue)
            .AsNoTracking()
            .ToListAsync();

        return predictions
            .Where(IsWinMatchPrediction)
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Count());
    }

    private static bool IsWinMatchPrediction(Prediction prediction)
    {
        if (prediction.Fixture == null)
        {
            return false;
        }

        if (!prediction.Fixture.TeamOneActualGoal.HasValue ||
            !prediction.Fixture.TeamTwoActualGoal.HasValue)
        {
            return false;
        }

        var predictedResult = Math.Sign(
            prediction.TeamOnePredictedGoal - prediction.TeamTwoPredictedGoal);

        var actualResult = Math.Sign(
            prediction.Fixture.TeamOneActualGoal.Value - prediction.Fixture.TeamTwoActualGoal.Value);

        return predictedResult == actualResult;
    }

    private static int RankSortValue(string? rankText)
    {
        if (string.IsNullOrWhiteSpace(rankText))
        {
            return 999999;
        }

        var clean = rankText.Replace("#", "").Trim();

        return int.TryParse(clean, out var value)
            ? value
            : 999999;
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

    [HttpGet]
    public IActionResult Rules()
    {
        return View();
    }
}