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

        settings.AutoActivateUserAfterEmailVerification = model.AutoActivateUserAfterEmailVerification;
        settings.ShowWhatsAppJoinMenu = model.ShowWhatsAppJoinMenu;
        settings.WhatsAppGroupUrl = model.WhatsAppGroupUrl;

        settings.ExecutionMode = model.ExecutionMode;

        settings.FixtureCheckIntervalMinutes = model.FixtureCheckIntervalMinutes;
        settings.ReminderCheckIntervalMinutes = model.ReminderCheckIntervalMinutes;
        settings.ScoreSyncIntervalMinutes = model.ScoreSyncIntervalMinutes;
        settings.AiUserReviewIntervalMinutes = model.AiUserReviewIntervalMinutes;
        settings.SessionCleanupIntervalMinutes = model.SessionCleanupIntervalMinutes;

        settings.WeeklyEmailSendDay = model.WeeklyEmailSendDay;
        settings.WeeklyEmailSendTime = model.WeeklyEmailSendTime;

        settings.DefaultMatchDurationMinutes = model.DefaultMatchDurationMinutes;

        /*
            Ads Settings
        */

        settings.AdsEnabled = model.AdsEnabled;

        settings.AdsShowOnDesktop = model.AdsShowOnDesktop;
        settings.AdsShowOnTablet = model.AdsShowOnTablet;
        settings.AdsShowOnMobile = model.AdsShowOnMobile;

        settings.AdsShowToGuests = model.AdsShowToGuests;
        settings.AdsShowToUsers = model.AdsShowToUsers;
        settings.AdsShowToAdmins = model.AdsShowToAdmins;

        settings.AdsShowOnDashboard = model.AdsShowOnDashboard;
        settings.AdsShowOnPredictionScore = model.AdsShowOnPredictionScore;
        settings.AdsShowOnLeaderboard = model.AdsShowOnLeaderboard;
        settings.AdsShowOnRules = model.AdsShowOnRules;
        settings.AdsShowOnLoginRegister = model.AdsShowOnLoginRegister;

        settings.AdsShowAfterSeconds = model.AdsShowAfterSeconds;
        settings.AdsMandatoryWatchSeconds = model.AdsMandatoryWatchSeconds;
        settings.AdsAutoCloseSeconds = model.AdsAutoCloseSeconds;
        settings.AdsDefaultSlideDurationSeconds = model.AdsDefaultSlideDurationSeconds;

        settings.AdsShowSkipButton = model.AdsShowSkipButton;
        settings.AdsShowCountdown = model.AdsShowCountdown;
        settings.AdsShowMuteButton = model.AdsShowMuteButton;
        settings.AdsRequireTapForSound = model.AdsRequireTapForSound;

        settings.AdsShowOncePerSession = model.AdsShowOncePerSession;
        settings.AdsShowOncePerDay = model.AdsShowOncePerDay;
        settings.AdsMaxImpressionsPerDayPerUser = model.AdsMaxImpressionsPerDayPerUser;

        settings.AdsTrackImpression = model.AdsTrackImpression;
        settings.AdsTrackSkip = model.AdsTrackSkip;
        settings.AdsTrackClick = model.AdsTrackClick;
        settings.AdsTrackComplete = model.AdsTrackComplete;
        settings.AdsTrackSoundEnabled = model.AdsTrackSoundEnabled;

        settings.AdsEnableSchedule = model.AdsEnableSchedule;
        settings.AdsStartAt = model.AdsStartAt;
        settings.AdsEndAt = model.AdsEndAt;


        settings.UpdatedAt = DateTime.Now;
        settings.UpdatedByUserId = _userManager.GetUserId(User);

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
            AutoActivateUserAfterEmailVerification = false,
            ShowWhatsAppJoinMenu = true,
            CreatedAt = DateTime.Now,

            AdsEnabled = false,

            AdsShowOnDesktop = true,
            AdsShowOnTablet = true,
            AdsShowOnMobile = true,

            AdsShowToGuests = true,
            AdsShowToUsers = true,
            AdsShowToAdmins = false,

            AdsShowOnDashboard = true,
            AdsShowOnPredictionScore = true,
            AdsShowOnLeaderboard = true,
            AdsShowOnRules = false,
            AdsShowOnLoginRegister = false,

            AdsShowAfterSeconds = 25,
            AdsMandatoryWatchSeconds = 5,
            AdsAutoCloseSeconds = 20,
            AdsDefaultSlideDurationSeconds = 4,

            AdsShowSkipButton = true,
            AdsShowCountdown = true,
            AdsShowMuteButton = true,
            AdsRequireTapForSound = true,

            AdsShowOncePerSession = true,
            AdsShowOncePerDay = false,
            AdsMaxImpressionsPerDayPerUser = 3,

            AdsTrackImpression = true,
            AdsTrackSkip = true,
            AdsTrackClick = true,
            AdsTrackComplete = true,
            AdsTrackSoundEnabled = true,

            AdsEnableSchedule = false,
            AdsStartAt = null,
            AdsEndAt = null,

            

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

            AutoActivateUserAfterEmailVerification = settings.AutoActivateUserAfterEmailVerification,
            ShowWhatsAppJoinMenu = settings.ShowWhatsAppJoinMenu,
            WhatsAppGroupUrl = settings.WhatsAppGroupUrl,

            ExecutionMode = settings.ExecutionMode,

            FixtureCheckIntervalMinutes = settings.FixtureCheckIntervalMinutes,
            ReminderCheckIntervalMinutes = settings.ReminderCheckIntervalMinutes,
            ScoreSyncIntervalMinutes = settings.ScoreSyncIntervalMinutes,
            AiUserReviewIntervalMinutes = settings.AiUserReviewIntervalMinutes,
            SessionCleanupIntervalMinutes = settings.SessionCleanupIntervalMinutes,

            WeeklyEmailSendDay = settings.WeeklyEmailSendDay,
            WeeklyEmailSendTime = settings.WeeklyEmailSendTime,
            DefaultMatchDurationMinutes = settings.DefaultMatchDurationMinutes,

            UpdatedAt = settings.UpdatedAt,

            AdsEnabled = settings.AdsEnabled,

            AdsShowOnDesktop = settings.AdsShowOnDesktop,
            AdsShowOnTablet = settings.AdsShowOnTablet,
            AdsShowOnMobile = settings.AdsShowOnMobile,

            AdsShowToGuests = settings.AdsShowToGuests,
            AdsShowToUsers = settings.AdsShowToUsers,
            AdsShowToAdmins = settings.AdsShowToAdmins,

            AdsShowOnDashboard = settings.AdsShowOnDashboard,
            AdsShowOnPredictionScore = settings.AdsShowOnPredictionScore,
            AdsShowOnLeaderboard = settings.AdsShowOnLeaderboard,
            AdsShowOnRules = settings.AdsShowOnRules,
            AdsShowOnLoginRegister = settings.AdsShowOnLoginRegister,

            AdsShowAfterSeconds = settings.AdsShowAfterSeconds,
            AdsMandatoryWatchSeconds = settings.AdsMandatoryWatchSeconds,
            AdsAutoCloseSeconds = settings.AdsAutoCloseSeconds,
            AdsDefaultSlideDurationSeconds = settings.AdsDefaultSlideDurationSeconds,

            AdsShowSkipButton = settings.AdsShowSkipButton,
            AdsShowCountdown = settings.AdsShowCountdown,
            AdsShowMuteButton = settings.AdsShowMuteButton,
            AdsRequireTapForSound = settings.AdsRequireTapForSound,

            AdsShowOncePerSession = settings.AdsShowOncePerSession,
            AdsShowOncePerDay = settings.AdsShowOncePerDay,
            AdsMaxImpressionsPerDayPerUser = settings.AdsMaxImpressionsPerDayPerUser,

            AdsTrackImpression = settings.AdsTrackImpression,
            AdsTrackSkip = settings.AdsTrackSkip,
            AdsTrackClick = settings.AdsTrackClick,
            AdsTrackComplete = settings.AdsTrackComplete,
            AdsTrackSoundEnabled = settings.AdsTrackSoundEnabled,

            AdsEnableSchedule = settings.AdsEnableSchedule,
            AdsStartAt = settings.AdsStartAt,
            AdsEndAt = settings.AdsEndAt,
        };
    }
}