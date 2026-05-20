using FootballPredictionGame.Models;
using FootballPredictionGame.Helpers;
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

    public LeaderboardController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var players = await _userManager.GetUsersInRoleAsync("User");

        var model = LeaderboardRankingHelper.Build(players, take: 100);

        return View(model);
    }
}
