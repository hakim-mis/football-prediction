using FootballPredictionGame.Data;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.Models;
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
        var players = await _userManager.GetUsersInRoleAsync("User");

        var playerIds = players
            .Select(x => x.Id)
            .ToList();

        var winMatchPredictionCountMap = await BuildWinMatchPredictionCountMapAsync(playerIds);

        var allRankedUsers = LeaderboardRankingHelper.Build(
            users: players,
            take: players.Count,
            scoredOnly: false,
            winMatchPredictionCountMap: winMatchPredictionCountMap);

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
}