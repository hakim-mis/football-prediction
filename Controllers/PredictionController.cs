using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Controllers;

[Authorize(Roles = "User")]
public class PredictionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PredictionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int fixtureId, int teamOneGoal, int teamTwoGoal)
    {
        if (teamOneGoal < 0 || teamTwoGoal < 0)
        {
            TempData["Error"] = "Prediction goals cannot be negative.";
            return RedirectToAction("Index", "Dashboard");
        }

        var userId = _userManager.GetUserId(User);
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null || !user.IsActive)
        {
            TempData["Error"] = "Your account is not active.";
            return RedirectToAction("Login", "Account");
        }

        if (user.MustChangePassword)
        {
            return RedirectToAction("ForceChangePassword", "Account");
        }

        var fixture = await _context.Fixtures.FirstOrDefaultAsync(x => x.Id == fixtureId && x.IsPublished);
        if (fixture == null)
        {
            TempData["Error"] = "Fixture not found.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Always re-check the fixture status from the database before saving.
        // Only Upcoming fixtures are editable. Live, Finished, and processed matches are locked.
        if (fixture.IsProcessed || fixture.Status != MatchStatus.Upcoming)
        {
            TempData["Error"] = "Prediction is locked. Only upcoming fixtures can be predicted or updated.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (DateTime.Now >= fixture.MatchDateTime)
        {
            TempData["Error"] = "Prediction time is over for this match.";
            return RedirectToAction("Index", "Dashboard");
        }

        var existingPrediction = await _context.Predictions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.FixtureId == fixtureId);

        if (existingPrediction == null)
        {
            var prediction = new Prediction
            {
                UserId = userId!,
                FixtureId = fixtureId,
                TeamOnePredictedGoal = teamOneGoal,
                TeamTwoPredictedGoal = teamTwoGoal,
                CreatedAt = DateTime.Now
            };

            _context.Predictions.Add(prediction);
            TempData["Success"] = "Prediction submitted successfully.";
        }
        else
        {
            existingPrediction.TeamOnePredictedGoal = teamOneGoal;
            existingPrediction.TeamTwoPredictedGoal = teamTwoGoal;
            existingPrediction.UpdatedAt = DateTime.Now;
            TempData["Success"] = "Prediction updated successfully.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Dashboard");
    }
}
