using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FixturesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public FixturesController(ApplicationDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    public async Task<IActionResult> Index()
    {
        var fixtures = await _context.Fixtures
            .OrderByDescending(x => x.MatchDateTime)
            .ToListAsync();

        return View(fixtures);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new FixtureFormViewModel
        {
            MatchDateTime = DateTime.Now.AddHours(1),
            IsPublished = true,
            Status = MatchStatus.Upcoming,
            Stage = FixtureStage.GroupA
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FixtureFormViewModel model)
    {
        if (model.TeamOneFlag == null)
        {
            ModelState.AddModelError(nameof(model.TeamOneFlag), "Team one flag is required.");
        }

        if (model.TeamTwoFlag == null)
        {
            ModelState.AddModelError(nameof(model.TeamTwoFlag), "Team two flag is required.");
        }

        ValidateFinishedScore(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? teamOneFlag = null;
        string? teamTwoFlag = null;

        try
        {
            teamOneFlag = await _fileUploadService.SaveImageAsync(model.TeamOneFlag!, "flags");
            teamTwoFlag = await _fileUploadService.SaveImageAsync(model.TeamTwoFlag!, "flags");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _fileUploadService.DeleteFileIfExists(teamOneFlag);
            _fileUploadService.DeleteFileIfExists(teamTwoFlag);
            return View(model);
        }

        var fixture = new Fixture
        {
            TeamOneName = model.TeamOneName.Trim(),
            TeamOneFlagPath = teamOneFlag,
            TeamTwoName = model.TeamTwoName.Trim(),
            TeamTwoFlagPath = teamTwoFlag,
            Stage = model.Stage,
            MatchDateTime = model.MatchDateTime,
            TeamOneActualGoal = model.TeamOneActualGoal,
            TeamTwoActualGoal = model.TeamTwoActualGoal,
            Status = model.Status,
            IsPublished = model.IsPublished,
            CreatedAt = DateTime.Now
        };

        _context.Fixtures.Add(fixture);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Fixture created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var fixture = await _context.Fixtures.FindAsync(id);
        if (fixture == null)
        {
            return NotFound();
        }

        return View(ToFormModel(fixture));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FixtureFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var fixture = await _context.Fixtures.FindAsync(id);
        if (fixture == null)
        {
            return NotFound();
        }

        ValidateFinishedScore(model);

        if (!ModelState.IsValid)
        {
            model.ExistingTeamOneFlagPath = fixture.TeamOneFlagPath;
            model.ExistingTeamTwoFlagPath = fixture.TeamTwoFlagPath;
            model.IsProcessed = fixture.IsProcessed;
            return View(model);
        }

        fixture.TeamOneName = model.TeamOneName.Trim();
        fixture.TeamTwoName = model.TeamTwoName.Trim();
        fixture.Stage = model.Stage;
        fixture.MatchDateTime = model.MatchDateTime;
        fixture.Status = model.Status;
        fixture.IsPublished = model.IsPublished;
        fixture.UpdatedAt = DateTime.Now;

        if (!fixture.IsProcessed)
        {
            fixture.TeamOneActualGoal = model.TeamOneActualGoal;
            fixture.TeamTwoActualGoal = model.TeamTwoActualGoal;
        }

        try
        {
            if (model.TeamOneFlag != null)
            {
                var newPath = await _fileUploadService.SaveImageAsync(model.TeamOneFlag, "flags");
                _fileUploadService.DeleteFileIfExists(fixture.TeamOneFlagPath);
                fixture.TeamOneFlagPath = newPath;
            }

            if (model.TeamTwoFlag != null)
            {
                var newPath = await _fileUploadService.SaveImageAsync(model.TeamTwoFlag, "flags");
                _fileUploadService.DeleteFileIfExists(fixture.TeamTwoFlagPath);
                fixture.TeamTwoFlagPath = newPath;
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ExistingTeamOneFlagPath = fixture.TeamOneFlagPath;
            model.ExistingTeamTwoFlagPath = fixture.TeamTwoFlagPath;
            model.IsProcessed = fixture.IsProcessed;
            return View(model);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Fixture updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var fixture = await _context.Fixtures
            .Include(x => x.Predictions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (fixture == null)
        {
            return NotFound();
        }

        if (fixture.Predictions.Any() || fixture.IsProcessed)
        {
            TempData["Error"] = "Fixture cannot be deleted because predictions or processed result records already exist.";
            return RedirectToAction(nameof(Index));
        }

        _fileUploadService.DeleteFileIfExists(fixture.TeamOneFlagPath);
        _fileUploadService.DeleteFileIfExists(fixture.TeamTwoFlagPath);
        _context.Fixtures.Remove(fixture);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Fixture deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static FixtureFormViewModel ToFormModel(Fixture fixture)
    {
        return new FixtureFormViewModel
        {
            Id = fixture.Id,
            TeamOneName = fixture.TeamOneName,
            ExistingTeamOneFlagPath = fixture.TeamOneFlagPath,
            TeamTwoName = fixture.TeamTwoName,
            ExistingTeamTwoFlagPath = fixture.TeamTwoFlagPath,
            Stage = fixture.Stage,
            MatchDateTime = fixture.MatchDateTime,
            TeamOneActualGoal = fixture.TeamOneActualGoal,
            TeamTwoActualGoal = fixture.TeamTwoActualGoal,
            Status = fixture.Status,
            IsPublished = fixture.IsPublished,
            IsProcessed = fixture.IsProcessed
        };
    }

    private void ValidateFinishedScore(FixtureFormViewModel model)
    {
        if (model.Status == MatchStatus.Finished && (model.TeamOneActualGoal == null || model.TeamTwoActualGoal == null))
        {
            ModelState.AddModelError(string.Empty, "Actual goals are required when match status is Finished.");
        }
    }
}
