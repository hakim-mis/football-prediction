// Areas/Admin/Controllers/AutomationSettingsController.cs
using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AutomationSettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AutomationSettingsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await GetOrCreateSettingsAsync();

        var model = ToViewModel(settings);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AutomationSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var settings = await GetOrCreateSettingsAsync();

        var beforeJson = System.Text.Json.JsonSerializer.Serialize(settings);

        settings.MasterAutomationEnabled = model.MasterAutomationEnabled;

        settings.FixtureAutomationEnabled = model.FixtureAutomationEnabled;
        settings.AutoUpcomingToLiveEnabled = model.AutoUpcomingToLiveEnabled;
        settings.AutoLiveToFinishedEnabled = model.AutoLiveToFinishedEnabled;
        settings.AutoResultProcessingEnabled = model.AutoResultProcessingEnabled;

        settings.ReminderAutomationEnabled = model.ReminderAutomationEnabled;
        settings.Reminder24HourEnabled = model.Reminder24HourEnabled;
        settings.Reminder1HourEnabled = model.Reminder1HourEnabled;

        settings.WeeklyEmailAutomationEnabled = model.WeeklyEmailAutomationEnabled;
        settings.WeeklyAppreciationEmailEnabled = model.WeeklyAppreciationEmailEnabled;
        settings.WeeklyImprovementEmailEnabled = model.WeeklyImprovementEmailEnabled;

        settings.WeeklyGoodPerformancePointThreshold = model.WeeklyGoodPerformancePointThreshold;
        settings.WeeklyPoorPerformancePointThreshold = model.WeeklyPoorPerformancePointThreshold;
        settings.WeeklyMissedPredictionThreshold = model.WeeklyMissedPredictionThreshold;

        settings.AiUserApprovalEnabled = model.AiUserApprovalEnabled;
        settings.AiAutoApproveEnabled = model.AiAutoApproveEnabled;
        settings.AiAutoApproveRiskThreshold = model.AiAutoApproveRiskThreshold;

        settings.LoginTrackingEnabled = model.LoginTrackingEnabled;
        settings.ActiveSessionTrackingEnabled = model.ActiveSessionTrackingEnabled;
        settings.SessionTimeoutMinutes = model.SessionTimeoutMinutes;

        settings.ExecutionMode = model.ExecutionMode;

        settings.FixtureCheckIntervalMinutes = model.FixtureCheckIntervalMinutes;
        settings.ReminderCheckIntervalMinutes = model.ReminderCheckIntervalMinutes;
        settings.ScoreSyncIntervalMinutes = model.ScoreSyncIntervalMinutes;
        settings.AiUserReviewIntervalMinutes = model.AiUserReviewIntervalMinutes;
        settings.SessionCleanupIntervalMinutes = model.SessionCleanupIntervalMinutes;

        settings.WeeklyEmailSendDay = model.WeeklyEmailSendDay;
        settings.WeeklyEmailSendTime = model.WeeklyEmailSendTime;

        settings.UpdatedAt = DateTime.Now;
        settings.UpdatedByUserId = _userManager.GetUserId(User);
        settings.DefaultMatchDurationMinutes = model.DefaultMatchDurationMinutes;

        var afterJson = System.Text.Json.JsonSerializer.Serialize(settings);

        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = "AutomationSettings",
            EntityType = "AutomationSettings",
            EntityId = settings.Id.ToString(),
            ActionName = "UpdateAutomationSettings",
            ExecutionMode = settings.ExecutionMode.ToString(),
            BeforeDataJson = beforeJson,
            AfterDataJson = afterJson,
            IsExecuted = true,
            IsSuccess = true,
            Message = "Automation settings updated by admin.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Automation settings updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<AutomationSettings> GetOrCreateSettingsAsync()
    {
        var settings = await _context.AutomationSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings != null)
        {
            return settings;
        }

        settings = new AutomationSettings
        {
            MasterAutomationEnabled = false,
            ExecutionMode = AutomationExecutionMode.SuggestOnly,
            CreatedAt = DateTime.Now
        };

        _context.AutomationSettings.Add(settings);
        await _context.SaveChangesAsync();

        return settings;
    }

    private static AutomationSettingsViewModel ToViewModel(AutomationSettings settings)
    {
        return new AutomationSettingsViewModel
        {
            Id = settings.Id,

            MasterAutomationEnabled = settings.MasterAutomationEnabled,

            FixtureAutomationEnabled = settings.FixtureAutomationEnabled,
            AutoUpcomingToLiveEnabled = settings.AutoUpcomingToLiveEnabled,
            AutoLiveToFinishedEnabled = settings.AutoLiveToFinishedEnabled,
            AutoResultProcessingEnabled = settings.AutoResultProcessingEnabled,

            ReminderAutomationEnabled = settings.ReminderAutomationEnabled,
            Reminder24HourEnabled = settings.Reminder24HourEnabled,
            Reminder1HourEnabled = settings.Reminder1HourEnabled,

            WeeklyEmailAutomationEnabled = settings.WeeklyEmailAutomationEnabled,
            WeeklyAppreciationEmailEnabled = settings.WeeklyAppreciationEmailEnabled,
            WeeklyImprovementEmailEnabled = settings.WeeklyImprovementEmailEnabled,

            WeeklyGoodPerformancePointThreshold = settings.WeeklyGoodPerformancePointThreshold,
            WeeklyPoorPerformancePointThreshold = settings.WeeklyPoorPerformancePointThreshold,
            WeeklyMissedPredictionThreshold = settings.WeeklyMissedPredictionThreshold,

            AiUserApprovalEnabled = settings.AiUserApprovalEnabled,
            AiAutoApproveEnabled = settings.AiAutoApproveEnabled,
            AiAutoApproveRiskThreshold = settings.AiAutoApproveRiskThreshold,

            LoginTrackingEnabled = settings.LoginTrackingEnabled,
            ActiveSessionTrackingEnabled = settings.ActiveSessionTrackingEnabled,
            SessionTimeoutMinutes = settings.SessionTimeoutMinutes,

            ExecutionMode = settings.ExecutionMode,

            FixtureCheckIntervalMinutes = settings.FixtureCheckIntervalMinutes,
            ReminderCheckIntervalMinutes = settings.ReminderCheckIntervalMinutes,
            ScoreSyncIntervalMinutes = settings.ScoreSyncIntervalMinutes,
            AiUserReviewIntervalMinutes = settings.AiUserReviewIntervalMinutes,
            SessionCleanupIntervalMinutes = settings.SessionCleanupIntervalMinutes,

            WeeklyEmailSendDay = settings.WeeklyEmailSendDay,
            WeeklyEmailSendTime = settings.WeeklyEmailSendTime,
            DefaultMatchDurationMinutes = settings.DefaultMatchDurationMinutes,

            UpdatedAt = settings.UpdatedAt
        };
    }
}