// Services/Automation/AutomationHealthCheckJob.cs
using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services.Automation;

public class AutomationHealthCheckJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;

    public AutomationHealthCheckJob(
        ApplicationDbContext context,
        IAutomationGuardService automationGuard)
    {
        _context = context;
        _automationGuard = automationGuard;
    }

    public async Task RunAsync()
    {
        var guard = await _automationGuard.CanRunAsync(AutomationFeature.FixtureAutomation);

        if (!guard.CanRun)
        {
            _context.AutomationLogs.Add(new AutomationLog
            {
                AutomationType = "HealthCheck",
                EntityType = "Automation",
                EntityId = null,
                ActionName = "AutomationHealthCheck",
                ExecutionMode = guard.ExecutionMode.ToString(),
                IsExecuted = false,
                IsSuccess = true,
                Message = guard.Message,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return;
        }

        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = "HealthCheck",
            EntityType = "Automation",
            EntityId = null,
            ActionName = "AutomationHealthCheck",
            ExecutionMode = guard.ExecutionMode.ToString(),
            IsExecuted = false,
            IsSuccess = true,
            Message = "Automation health check completed. No data was changed.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}