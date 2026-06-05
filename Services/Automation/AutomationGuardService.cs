// Services/Automation/AutomationGuardService.cs
using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services.Automation;

public class AutomationGuardService : IAutomationGuardService
{
    private readonly ApplicationDbContext _context;

    public AutomationGuardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AutomationGuardResult> CanRunAsync(AutomationFeature feature)
    {
        var settings = await GetSettingsAsync();

        if (!settings.MasterAutomationEnabled)
        {
            return AutomationGuardResult.Blocked(
                "Master automation switch is OFF.",
                settings
            );
        }

        if (!IsFeatureEnabled(settings, feature))
        {
            return AutomationGuardResult.Blocked(
                $"{feature} is disabled in automation settings.",
                settings
            );
        }

        if (settings.ExecutionMode == AutomationExecutionMode.Manual)
        {
            return AutomationGuardResult.Blocked(
                "Automation execution mode is Manual.",
                settings
            );
        }

        return AutomationGuardResult.Allowed(settings);
    }

    public async Task<AutomationSettings> GetSettingsAsync()
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

    public async Task<bool> IsMasterEnabledAsync()
    {
        var settings = await GetSettingsAsync();

        return settings.MasterAutomationEnabled;
    }

    public async Task<bool> IsFeatureEnabledAsync(AutomationFeature feature)
    {
        var settings = await GetSettingsAsync();

        return IsFeatureEnabled(settings, feature);
    }

    private static bool IsFeatureEnabled(
        AutomationSettings settings,
        AutomationFeature feature)
    {
        return feature switch
        {
            AutomationFeature.FixtureAutomation =>
                settings.FixtureAutomationEnabled,

            AutomationFeature.ReminderAutomation =>
                settings.ReminderAutomationEnabled,

            AutomationFeature.WeeklyEmailAutomation =>
                settings.WeeklyEmailAutomationEnabled,

            AutomationFeature.AiUserApproval =>
                settings.AiUserApprovalEnabled,

            AutomationFeature.LoginTracking =>
                settings.LoginTrackingEnabled,

            AutomationFeature.ActiveSessionTracking =>
                settings.ActiveSessionTrackingEnabled,

            AutomationFeature.ScoreSync =>
                settings.FixtureAutomationEnabled,

            AutomationFeature.ResultProcessing =>
                settings.FixtureAutomationEnabled &&
                settings.AutoResultProcessingEnabled,

            _ => false
        };
    }
}