// Services/Automation/IAutomationGuardService.cs
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;

namespace FootballPredictionGame.Services.Automation;

public interface IAutomationGuardService
{
    Task<AutomationGuardResult> CanRunAsync(AutomationFeature feature);

    Task<AutomationSettings> GetSettingsAsync();

    Task<bool> IsMasterEnabledAsync();

    Task<bool> IsFeatureEnabledAsync(AutomationFeature feature);
}