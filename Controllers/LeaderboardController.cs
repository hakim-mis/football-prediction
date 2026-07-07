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

[Authorize]
public class LeaderboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public LeaderboardController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string range = "1-50")
    {
        var allRankedUsers = await BuildAllLeaderboardUsersAsync();

        var filteredModel = range switch
        {
            "51-100" => allRankedUsers.Skip(50).Take(50).ToList(),
            "101-150" => allRankedUsers.Skip(100).Take(50).ToList(),
            "rest" => allRankedUsers.Skip(150).ToList(),
            _ => allRankedUsers.Take(50).ToList()
        };

        var startSl = range switch
        {
            "51-100" => 51,
            "101-150" => 101,
            "rest" => 151,
            _ => 1
        };

        ViewBag.Range = range;
        ViewBag.StartSl = startSl;

        return View(filteredModel);
    }

    public async Task<IActionResult> DownloadExcel()
    {
        var allRankedUsers = await BuildAllLeaderboardUsersAsync();

        var totalPlayableMatchCount = await GetTotalPlayableMatchCountAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Leaderboard");

        worksheet.Cell(1, 1).Value = "SL";
        worksheet.Cell(1, 2).Value = "Rank";
        worksheet.Cell(1, 3).Value = "Player";
        worksheet.Cell(1, 4).Value = "Designation";
        worksheet.Cell(1, 5).Value = "Department";
        worksheet.Cell(1, 6).Value = "Score";
        worksheet.Cell(1, 7).Value = "Exact";
        worksheet.Cell(1, 8).Value = "Win";
        //worksheet.Cell(1, 9).Value = $"Played Out Of {totalPlayableMatchCount}";
        worksheet.Cell(1, 9).Value = $"Played/{totalPlayableMatchCount}";
        worksheet.Cell(1, 10).Value = "Not Played";

        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var row = 2;
        var sl = 1;

        foreach (var user in allRankedUsers)
        {
            worksheet.Cell(row, 1).Value = sl;
            worksheet.Cell(row, 2).Value = user.RankText;
            worksheet.Cell(row, 3).Value = user.FullName;
            worksheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(user.Designation) ? "-" : user.Designation;
            worksheet.Cell(row, 5).Value = string.IsNullOrWhiteSpace(user.Department) ? "-" : user.Department;
            worksheet.Cell(row, 6).Value = user.TotalScore;
            worksheet.Cell(row, 7).Value = user.ExactPredictionCount;
            worksheet.Cell(row, 8).Value = user.WinMatchPredictionCount;
            //worksheet.Cell(row, 9).Value = $"{user.PlayedMatchCount} / {user.TotalPlayableMatchCount}";
            worksheet.Cell(row, 9).Value = $"{user.PlayedMatchCount}";
            worksheet.Cell(row, 10).Value = user.NotPlayedMatchCount;

            row++;
            sl++;
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 10;
        worksheet.Column(3).Width = 28;
        worksheet.Column(4).Width = 28;
        worksheet.Column(5).Width = 28;
        worksheet.Column(6).Width = 14;
        worksheet.Column(7).Width = 10;
        worksheet.Column(8).Width = 10;
        worksheet.Column(9).Width = 20;
        worksheet.Column(10).Width = 14;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Leaderboard_All_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task<List<LeaderboardUserViewModel>> BuildAllLeaderboardUsersAsync()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var playerIds = players
            .Select(x => x.Id)
            .ToList();

        var totalPlayableMatchCount = await GetTotalPlayableMatchCountAsync();

        var playedMatchCountMap = await BuildPlayedMatchCountMapAsync(playerIds);

        var winMatchPredictionCountMap = await BuildWinMatchPredictionCountMapAsync(playerIds);

        var allRankedUsers = LeaderboardRankingHelper.Build(
            users: players,
            take: players.Count,
            scoredOnly: false,
            winMatchPredictionCountMap: winMatchPredictionCountMap);

        foreach (var user in allRankedUsers)
        {
            var playedCount = playedMatchCountMap.TryGetValue(user.UserId, out var value)
                ? value
                : 0;

            user.PlayedMatchCount = playedCount;
            user.TotalPlayableMatchCount = totalPlayableMatchCount;
            user.NotPlayedMatchCount = Math.Max(totalPlayableMatchCount - playedCount, 0);
        }

        return allRankedUsers;
    }

    private async Task<int> GetTotalPlayableMatchCountAsync()
    {
        return await _context.Fixtures
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == MatchStatus.Finished ||
                x.Status == MatchStatus.Live);
    }

    private async Task<Dictionary<string, int>> BuildPlayedMatchCountMapAsync(List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            return new Dictionary<string, int>();
        }

        return await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x =>
                userIds.Contains(x.UserId) &&
                x.Fixture != null &&
                (
                    x.Fixture.Status == MatchStatus.Finished ||
                    x.Fixture.Status == MatchStatus.Live
                ))
            .AsNoTracking()
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Select(x => x.FixtureId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);
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
                x.Fixture != null &&
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
}