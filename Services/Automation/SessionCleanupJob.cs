using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services.Automation;

public class SessionCleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;
    private readonly ILoginTrackingService _loginTrackingService;

    public SessionCleanupJob(
        ApplicationDbContext context,
        IAutomationGuardService automationGuard,
        ILoginTrackingService loginTrackingService)
    {
        _context = context;
        _automationGuard = automationGuard;
        _loginTrackingService = loginTrackingService;
    }

    public async Task RunAsync()
    {
        var guard = await _automationGuard.CanRunAsync(AutomationFeature.ActiveSessionTracking);

        if (!guard.CanRun)
        {
            await WriteLogAsync(
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: false,
                isSuccess: true,
                message: guard.Message,
                errorMessage: null
            );

            return;
        }

        try
        {
            var settings = guard.Settings ?? await _automationGuard.GetSettingsAsync();

            var timeoutMinutes = settings.SessionTimeoutMinutes <= 0
                ? 10
                : settings.SessionTimeoutMinutes;

            var now = DateTime.Now;
            var expiryTime = now.AddMinutes(-timeoutMinutes);

            var expiredCount = await _context.UserActiveSessions
                .CountAsync(x =>
                    x.IsActive &&
                    x.LastSeenAt < expiryTime);

            if (guard.CanSuggest)
            {
                await WriteLogAsync(
                    executionMode: guard.ExecutionMode.ToString(),
                    isExecuted: false,
                    isSuccess: true,
                    message: $"Session cleanup suggestion only. Expired active sessions found: {expiredCount}.",
                    errorMessage: null
                );

                return;
            }

            if (!guard.CanExecute)
            {
                await WriteLogAsync(
                    executionMode: guard.ExecutionMode.ToString(),
                    isExecuted: false,
                    isSuccess: true,
                    message: "Session cleanup skipped because execution mode does not allow auto execution.",
                    errorMessage: null
                );

                return;
            }

            await _loginTrackingService.CleanupExpiredSessionsAsync(timeoutMinutes);

            await WriteLogAsync(
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: true,
                isSuccess: true,
                message: $"Session cleanup completed. Expired active sessions processed: {expiredCount}.",
                errorMessage: null
            );
        }
        catch (Exception ex)
        {
            await WriteLogAsync(
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: true,
                isSuccess: false,
                message: "Session cleanup failed.",
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private async Task WriteLogAsync(
        string executionMode,
        bool isExecuted,
        bool isSuccess,
        string message,
        string? errorMessage)
    {
        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = "SessionCleanup",
            EntityType = "UserActiveSession",
            EntityId = null,
            ActionName = "CleanupExpiredSessions",
            ExecutionMode = executionMode,
            IsExecuted = isExecuted,
            IsSuccess = isSuccess,
            Message = message,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}