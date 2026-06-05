using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ResultProcessingController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IResultProcessingService _resultProcessingService;

    public ResultProcessingController(
        UserManager<ApplicationUser> userManager,
        IResultProcessingService resultProcessingService)
    {
        _userManager = userManager;
        _resultProcessingService = resultProcessingService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id)
    {
        var result = await _resultProcessingService.ProcessAsync(
            fixtureId: id,
            processedByUserId: _userManager.GetUserId(User),
            source: "Manual"
        );

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Result processing failed.";
        }

        return RedirectToAction(
            result.RedirectAction,
            result.RedirectController,
            result.RedirectRouteValues
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(int id)
    {
        var result = await _resultProcessingService.UndoAsync(
            fixtureId: id,
            processedByUserId: _userManager.GetUserId(User),
            source: "Manual"
        );

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Undo processing failed.";
        }

        return RedirectToAction(
            result.RedirectAction,
            result.RedirectController,
            result.RedirectRouteValues
        );
    }
}