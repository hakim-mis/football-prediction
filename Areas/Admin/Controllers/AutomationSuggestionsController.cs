using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AutomationSuggestionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IResultProcessingService _resultProcessingService;
    private readonly IPredictionReminderEmailService _predictionReminderEmailService;
    private readonly IWeeklyPerformanceEmailService _weeklyPerformanceEmailService;

    public AutomationSuggestionsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IResultProcessingService resultProcessingService,
        IPredictionReminderEmailService predictionReminderEmailService,
        IWeeklyPerformanceEmailService weeklyPerformanceEmailService)
    {
        _context = context;
        _userManager = userManager;
        _resultProcessingService = resultProcessingService;
        _predictionReminderEmailService = predictionReminderEmailService;
        _weeklyPerformanceEmailService = weeklyPerformanceEmailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string status = "pending")
    {
        var query = _context.AutomationSuggestions.AsQueryable();

        if (status == "pending")
        {
            query = query.Where(x => !x.IsReviewed);
        }
        else if (status == "executed")
        {
            query = query.Where(x => x.IsReviewed && x.IsApproved);
        }
        else if (status == "rejected")
        {
            query = query.Where(x => x.IsReviewed && x.IsRejected);
        }

        var model = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .Select(x => new AutomationSuggestionItemViewModel
            {
                Id = x.Id,
                AutomationType = x.AutomationType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                SuggestedAction = x.SuggestedAction,
                Reason = x.Reason,
                ConfidenceScore = x.ConfidenceScore,
                IsReviewed = x.IsReviewed,
                IsApproved = x.IsApproved,
                IsRejected = x.IsRejected,
                CreatedAt = x.CreatedAt,
                ReviewedAt = x.ReviewedAt
            })
            .ToListAsync();

        ViewBag.Status = status;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id)
    {
        var suggestion = await _context.AutomationSuggestions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (suggestion == null)
        {
            return NotFound();
        }

        if (suggestion.IsReviewed)
        {
            TempData["Error"] = "This suggestion is already reviewed.";
            return RedirectToAction(nameof(Index));
        }

        suggestion.IsReviewed = true;
        suggestion.IsRejected = true;
        suggestion.IsApproved = false;
        suggestion.ReviewedAt = DateTime.Now;
        suggestion.ReviewedByUserId = _userManager.GetUserId(User);

        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = suggestion.AutomationType,
            EntityType = suggestion.EntityType,
            EntityId = suggestion.EntityId,
            ActionName = "RejectSuggestion",
            ExecutionMode = "Manual",
            BeforeDataJson = suggestion.BeforeDataJson,
            AfterDataJson = suggestion.SuggestedDataJson,
            IsExecuted = false,
            IsSuccess = true,
            Message = $"Suggestion rejected by admin. Suggested action: {suggestion.SuggestedAction}",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Suggestion rejected successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(long id)
    {
        var suggestion = await _context.AutomationSuggestions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (suggestion == null)
        {
            return NotFound();
        }

        if (suggestion.IsReviewed)
        {
            TempData["Error"] = "This suggestion is already reviewed.";
            return RedirectToAction(nameof(Index));
        }

        var executionResult = await ExecuteSuggestionAsync(suggestion);

        suggestion.IsReviewed = true;
        suggestion.IsApproved = executionResult.Success;
        suggestion.IsRejected = !executionResult.Success;
        suggestion.ReviewedAt = DateTime.Now;
        suggestion.ReviewedByUserId = _userManager.GetUserId(User);

        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = suggestion.AutomationType,
            EntityType = suggestion.EntityType,
            EntityId = suggestion.EntityId,
            ActionName = suggestion.SuggestedAction,
            ExecutionMode = "Manual",
            BeforeDataJson = suggestion.BeforeDataJson,
            AfterDataJson = suggestion.SuggestedDataJson,
            IsExecuted = executionResult.Success,
            IsSuccess = executionResult.Success,
            Message = executionResult.Message,
            ErrorMessage = executionResult.ErrorMessage,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        if (executionResult.Success)
        {
            TempData["Success"] = executionResult.Message;
        }
        else
        {
            TempData["Error"] = executionResult.ErrorMessage ?? "Suggestion execution failed.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<SuggestionExecutionResult> ExecuteSuggestionAsync(AutomationSuggestion suggestion)
    {
        if (suggestion.AutomationType == "FixtureStatus" &&
            suggestion.EntityType == "Fixture" &&
            suggestion.SuggestedAction == "UpcomingToLive")
        {
            return await ExecuteUpcomingToLiveAsync(suggestion);
        }

        if (suggestion.AutomationType == "FixtureStatus" &&
            suggestion.EntityType == "Fixture" &&
            suggestion.SuggestedAction == "LiveToFinished")
        {
            return await ExecuteLiveToFinishedAsync(suggestion);
        }

        if (suggestion.AutomationType == "ResultProcessing" &&
            suggestion.EntityType == "Fixture" &&
            suggestion.SuggestedAction == "ProcessResult")
        {
            return await ExecuteProcessResultAsync(suggestion);
        }

        if (suggestion.AutomationType == "AiUserApproval" &&
            suggestion.EntityType == "User" &&
            suggestion.SuggestedAction == "ApproveUser")
        {
            return await ExecuteApproveUserAsync(suggestion);
        }

        if (suggestion.AutomationType == "PredictionReminder" &&
            suggestion.EntityType == "Email" &&
            (suggestion.SuggestedAction == "Send24HourReminder" ||
            suggestion.SuggestedAction == "Send1HourReminder"))
        {
            return await ExecutePredictionReminderAsync(suggestion);
        }

        if (suggestion.AutomationType == "WeeklyPerformanceEmail" &&
            suggestion.EntityType == "Email" &&
            (suggestion.SuggestedAction == "SendWeeklyAppreciationEmail" ||
            suggestion.SuggestedAction == "SendWeeklyImprovementEmail"))
        {
            return await ExecuteWeeklyPerformanceEmailAsync(suggestion);
        }
        return new SuggestionExecutionResult
        {
            Success = false,
            ErrorMessage = $"No executor found for {suggestion.AutomationType} / {suggestion.SuggestedAction}."
        };
    }

    private async Task<SuggestionExecutionResult> ExecuteUpcomingToLiveAsync(AutomationSuggestion suggestion)
    {
        if (!int.TryParse(suggestion.EntityId, out var fixtureId))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid fixture id."
            };
        }

        var fixture = await _context.Fixtures.FirstOrDefaultAsync(x => x.Id == fixtureId);

        if (fixture == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Fixture not found."
            };
        }

        if (fixture.Status != MatchStatus.Upcoming)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Fixture is no longer Upcoming."
            };
        }

        fixture.Status = MatchStatus.Live;
        fixture.UpdatedAt = DateTime.Now;

        return new SuggestionExecutionResult
        {
            Success = true,
            Message = "Fixture status updated from Upcoming to Live."
        };
    }

    private async Task<SuggestionExecutionResult> ExecuteLiveToFinishedAsync(AutomationSuggestion suggestion)
    {
        if (!int.TryParse(suggestion.EntityId, out var fixtureId))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid fixture id."
            };
        }

        var fixture = await _context.Fixtures.FirstOrDefaultAsync(x => x.Id == fixtureId);

        if (fixture == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Fixture not found."
            };
        }

        if (fixture.Status != MatchStatus.Live)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Fixture is no longer Live."
            };
        }

        fixture.Status = MatchStatus.Finished;
        fixture.UpdatedAt = DateTime.Now;

        return new SuggestionExecutionResult
        {
            Success = true,
            Message = "Fixture status updated from Live to Finished."
        };
    }

    private async Task<SuggestionExecutionResult> ExecuteProcessResultAsync(AutomationSuggestion suggestion)
    {
        if (!int.TryParse(suggestion.EntityId, out var fixtureId))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid fixture id."
            };
        }

        var result = await _resultProcessingService.ProcessAsync(
            fixtureId: fixtureId,
            processedByUserId: _userManager.GetUserId(User),
            source: "SuggestionManual"
        );

        return new SuggestionExecutionResult
        {
            Success = result.Success,
            Message = result.Message,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<SuggestionExecutionResult> ExecuteApproveUserAsync(AutomationSuggestion suggestion)
    {
        var userId = suggestion.EntityId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid user id."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User not found."
            };
        }

        if (!user.EmailConfirmed)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User email is not verified."
            };
        }

        if (user.IsActive)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User is already active."
            };
        }

        user.IsActive = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = string.Join(", ", result.Errors.Select(x => x.Description))
            };
        }

        return new SuggestionExecutionResult
        {
            Success = true,
            Message = "User approved successfully from automation suggestion."
        };
    }

    private async Task<SuggestionExecutionResult> ExecutePredictionReminderAsync(AutomationSuggestion suggestion)
    {
        var parts = suggestion.EntityId?.Split(':');

        if (parts == null || parts.Length != 3)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid reminder suggestion entity id."
            };
        }

        if (!int.TryParse(parts[0], out var fixtureId))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid fixture id."
            };
        }

        var userId = parts[1];
        var reminderType = parts[2];

        var fixture = await _context.Fixtures
            .FirstOrDefaultAsync(x => x.Id == fixtureId);

        if (fixture == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Fixture not found."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User not found."
            };
        }

        var alreadyPredicted = await _context.Predictions
            .AnyAsync(x =>
                x.FixtureId == fixture.Id &&
                x.UserId == user.Id);

        if (alreadyPredicted)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User already submitted prediction."
            };
        }

        var alreadySent = await _context.PredictionReminderLogs
            .AnyAsync(x =>
                x.UserId == user.Id &&
                x.FixtureId == fixture.Id &&
                x.ReminderType == reminderType &&
                x.IsSent);

        if (alreadySent)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Reminder email already sent."
            };
        }

        await _predictionReminderEmailService.SendPredictionReminderAsync(
            user,
            fixture,
            reminderType
        );

        _context.PredictionReminderLogs.Add(new PredictionReminderLog
        {
            UserId = user.Id,
            FixtureId = fixture.Id,
            ReminderType = reminderType,
            EmailTo = user.Email,
            IsSent = true,
            SentAt = DateTime.Now,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return new SuggestionExecutionResult
        {
            Success = true,
            Message = $"{reminderType} reminder email sent to {user.Email}."
        };
    }

    private async Task<SuggestionExecutionResult> ExecuteWeeklyPerformanceEmailAsync(AutomationSuggestion suggestion)
    {
        var parts = suggestion.EntityId?.Split(':');

        if (parts == null || parts.Length != 4)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid weekly email suggestion entity id."
            };
        }

        var userId = parts[0];
        var emailType = parts[1];

        if (!DateTime.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var weekStart))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid week start date."
            };
        }

        if (!DateTime.TryParseExact(parts[3], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var weekEnd))
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid week end date."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "User not found."
            };
        }

        var existingSent = await _context.WeeklyPerformanceEmailLogs
            .AnyAsync(x =>
                x.UserId == user.Id &&
                x.EmailType == emailType &&
                x.WeekStartDate == weekStart &&
                x.WeekEndDate == weekEnd &&
                x.IsSent);

        if (existingSent)
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Weekly performance email already sent."
            };
        }

        var weekEndExclusive = weekEnd.AddDays(1);

        var weeklyPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x =>
                x.UserId == user.Id &&
                x.IsProcessed &&
                x.Fixture.MatchDateTime >= weekStart &&
                x.Fixture.MatchDateTime < weekEndExclusive)
            .ToListAsync();

        var finishedPublishedFixtureIds = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                x.Status == MatchStatus.Finished &&
                x.MatchDateTime >= weekStart &&
                x.MatchDateTime < weekEndExclusive)
            .Select(x => x.Id)
            .ToListAsync();

        var predictedFixtureIds = weeklyPredictions
            .Select(x => x.FixtureId)
            .Distinct()
            .ToList();

        var missedPredictionCount = finishedPublishedFixtureIds
            .Count(x => !predictedFixtureIds.Contains(x));

        var weeklyPoint = weeklyPredictions.Sum(x => x.EarnedPoint);
        var predictionCount = weeklyPredictions.Count;
        var exactPredictionCount = weeklyPredictions.Count(x => x.EarnedPoint == 3);

        if (emailType == "Appreciation")
        {
            await _weeklyPerformanceEmailService.SendAppreciationEmailAsync(
                user,
                weeklyPoint,
                predictionCount,
                exactPredictionCount,
                missedPredictionCount,
                weekStart,
                weekEnd
            );
        }
        else if (emailType == "Improvement")
        {
            await _weeklyPerformanceEmailService.SendImprovementEmailAsync(
                user,
                weeklyPoint,
                predictionCount,
                exactPredictionCount,
                missedPredictionCount,
                weekStart,
                weekEnd
            );
        }
        else
        {
            return new SuggestionExecutionResult
            {
                Success = false,
                ErrorMessage = "Invalid weekly email type."
            };
        }

        _context.WeeklyPerformanceEmailLogs.Add(new WeeklyPerformanceEmailLog
        {
            UserId = user.Id,
            EmailTo = user.Email,
            EmailType = emailType,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            WeeklyPoint = weeklyPoint,
            WeeklyPredictionCount = predictionCount,
            WeeklyExactPredictionCount = exactPredictionCount,
            MissedPredictionCount = missedPredictionCount,
            IsSent = true,
            SentAt = DateTime.Now,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return new SuggestionExecutionResult
        {
            Success = true,
            Message = $"Weekly {emailType} email sent to {user.Email}."
        };
    }

    private class SuggestionExecutionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}