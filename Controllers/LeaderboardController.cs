using FootballPredictionGame.Models;
using FootballPredictionGame.Helpers;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FootballPredictionGame.Controllers;

[Authorize]
public class LeaderboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LeaderboardController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string range = "1-50")
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var allRankedUsers = LeaderboardRankingHelper.Build(players, take: players.Count);

        var startSl = 1;

        var filteredModel = range switch
        {
            "51-100" => allRankedUsers.Skip(50).Take(50).ToList(),
            "101-150" => allRankedUsers.Skip(100).Take(50).ToList(),
            "rest" => allRankedUsers.Skip(150).ToList(),
            _ => allRankedUsers.Take(50).ToList()
        };

        startSl = range switch
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
}