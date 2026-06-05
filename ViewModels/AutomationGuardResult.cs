// ViewModels/AutomationGuardResult.cs
using FootballPredictionGame.Models;

namespace FootballPredictionGame.ViewModels;

public class AutomationGuardResult
{
    public bool CanRun { get; set; }

    public bool CanExecute { get; set; }

    public bool CanSuggest { get; set; }

    public string Message { get; set; } = string.Empty;

    public AutomationExecutionMode ExecutionMode { get; set; }

    public AutomationSettings? Settings { get; set; }

    public static AutomationGuardResult Blocked(string message, AutomationSettings? settings = null)
    {
        return new AutomationGuardResult
        {
            CanRun = false,
            CanExecute = false,
            CanSuggest = false,
            Message = message,
            ExecutionMode = settings?.ExecutionMode ?? AutomationExecutionMode.Manual,
            Settings = settings
        };
    }

    public static AutomationGuardResult Allowed(AutomationSettings settings)
    {
        return new AutomationGuardResult
        {
            CanRun = true,
            CanExecute = settings.ExecutionMode == AutomationExecutionMode.AutoExecute,
            CanSuggest = settings.ExecutionMode == AutomationExecutionMode.SuggestOnly,
            Message = "Automation is allowed.",
            ExecutionMode = settings.ExecutionMode,
            Settings = settings
        };
    }
}