using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ResultProcessingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResultProcessingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var fixture = await _context.Fixtures
            .Include(x => x.Predictions)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (fixture == null)
        {
            return NotFound();
        }

        if (fixture.IsProcessed)
        {
            TempData["Error"] = "This fixture has already been processed.";
            return RedirectToAction("Index", "Fixtures", new { area = "Admin" });
        }

        if (fixture.TeamOneActualGoal == null || fixture.TeamTwoActualGoal == null)
        {
            TempData["Error"] = "Please enter the actual match score before processing.";
            return RedirectToAction("Edit", "Fixtures", new { area = "Admin", id });
        }

        var unprocessedPredictions = fixture.Predictions.Where(x => !x.IsProcessed).ToList();

        foreach (var prediction in unprocessedPredictions)
        {
            var point = CalculatePoint(
                prediction.TeamOnePredictedGoal,
                prediction.TeamTwoPredictedGoal,
                fixture.TeamOneActualGoal.Value,
                fixture.TeamTwoActualGoal.Value);

            prediction.EarnedPoint = point;
            prediction.IsProcessed = true;
            prediction.UpdatedAt = DateTime.Now;

            prediction.User.TotalScore += point;
            if (point == 3)
            {
                prediction.User.ExactPredictionCount += 1;
            }
        }

        fixture.IsProcessed = true;
        fixture.Status = MatchStatus.Finished;
        fixture.UpdatedAt = DateTime.Now;

        _context.ResultProcessingLogs.Add(new ResultProcessingLog
        {
            FixtureId = fixture.Id,
            ProcessedAt = DateTime.Now,
            ProcessedByUserId = _userManager.GetUserId(User),
            TotalPredictionsProcessed = unprocessedPredictions.Count
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = $"Result processed successfully. {unprocessedPredictions.Count} predictions updated.";
        return RedirectToAction("Index", "Fixtures", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var fixture = await _context.Fixtures
            .Include(x => x.Predictions)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (fixture == null)
        {
            return NotFound();
        }

        if (!fixture.IsProcessed)
        {
            TempData["Error"] = "This fixture is not processed yet.";
            return RedirectToAction("Index", "Fixtures", new { area = "Admin" });
        }

        var processedPredictions = fixture.Predictions.Where(x => x.IsProcessed).ToList();

        foreach (var prediction in processedPredictions)
        {
            var previousPoint = prediction.EarnedPoint;

            prediction.User.TotalScore = Math.Max(0, prediction.User.TotalScore - previousPoint);
            if (previousPoint == 3)
            {
                prediction.User.ExactPredictionCount = Math.Max(0, prediction.User.ExactPredictionCount - 1);
            }

            prediction.EarnedPoint = 0;
            prediction.IsProcessed = false;
            prediction.UpdatedAt = DateTime.Now;
        }

        var logs = await _context.ResultProcessingLogs
            .Where(x => x.FixtureId == fixture.Id)
            .ToListAsync();
        _context.ResultProcessingLogs.RemoveRange(logs);

        fixture.IsProcessed = false;
        fixture.Status = MatchStatus.Finished;
        fixture.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = $"Processing undone successfully. {processedPredictions.Count} prediction score(s) were reverted. You can now edit the actual goals and process again.";
        return RedirectToAction("Edit", "Fixtures", new { area = "Admin", id });
    }

    private static int CalculatePoint(int predictedOne, int predictedTwo, int actualOne, int actualTwo)
    {
        if (predictedOne == actualOne && predictedTwo == actualTwo)
        {
            return 3;
        }

        var predictedResult = predictedOne.CompareTo(predictedTwo);
        var actualResult = actualOne.CompareTo(actualTwo);

        return predictedResult == actualResult ? 1 : 0;
    }
}
