using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FootballPredictionGame.Services.Automation;

public class FixtureStatusAutomationJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;

    public FixtureStatusAutomationJob(
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
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "FixtureStatusAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
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

            await HandleUpcomingToLiveAsync(guard);
            await HandleLiveToFinishedAsync(guard, settings.DefaultMatchDurationMinutes);
        }
        catch (Exception ex)
        {
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "FixtureStatusAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: true,
                isSuccess: false,
                message: "Fixture status automation failed.",
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private async Task HandleUpcomingToLiveAsync(AutomationGuardResult guard)
    {
        var settings = guard.Settings ?? await _automationGuard.GetSettingsAsync();

        if (!settings.AutoUpcomingToLiveEnabled)
        {
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "UpcomingToLive",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: false,
                isSuccess: true,
                message: "Auto Upcoming to Live is disabled.",
                errorMessage: null
            );

            return;
        }

        var now = DateTime.Now;

        var fixtures = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                x.Status == MatchStatus.Upcoming &&
                x.MatchDateTime <= now)
            .OrderBy(x => x.MatchDateTime)
            .ToListAsync();

        if (!fixtures.Any())
        {
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "UpcomingToLive",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: false,
                isSuccess: true,
                message: "No Upcoming fixture found to change to Live.",
                errorMessage: null
            );

            return;
        }

        foreach (var fixture in fixtures)
        {
            var beforeJson = JsonSerializer.Serialize(new
            {
                fixture.Id,
                fixture.TeamOneName,
                fixture.TeamTwoName,
                fixture.Status,
                fixture.MatchDateTime
            });

            var afterJson = JsonSerializer.Serialize(new
            {
                fixture.Id,
                NewStatus = MatchStatus.Live.ToString()
            });

            if (guard.CanSuggest)
            {
                await CreateSuggestionIfNotExistsAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    suggestedAction: "UpcomingToLive",
                    reason: $"{fixture.TeamOneName} vs {fixture.TeamTwoName} should be Live because match time has started.",
                    confidenceScore: 100,
                    beforeJson: beforeJson,
                    suggestedJson: afterJson
                );

                await WriteLogAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    actionName: "UpcomingToLive",
                    executionMode: guard.ExecutionMode.ToString(),
                    beforeDataJson: beforeJson,
                    afterDataJson: afterJson,
                    isExecuted: false,
                    isSuccess: true,
                    message: "Upcoming to Live suggestion created.",
                    errorMessage: null
                );

                continue;
            }

            if (guard.CanExecute)
            {
                fixture.Status = MatchStatus.Live;

                await WriteLogAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    actionName: "UpcomingToLive",
                    executionMode: guard.ExecutionMode.ToString(),
                    beforeDataJson: beforeJson,
                    afterDataJson: afterJson,
                    isExecuted: true,
                    isSuccess: true,
                    message: $"{fixture.TeamOneName} vs {fixture.TeamTwoName} changed from Upcoming to Live.",
                    errorMessage: null
                );
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandleLiveToFinishedAsync(
        AutomationGuardResult guard,
        int defaultMatchDurationMinutes)
    {
        var settings = guard.Settings ?? await _automationGuard.GetSettingsAsync();

        if (!settings.AutoLiveToFinishedEnabled)
        {
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "LiveToFinished",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: false,
                isSuccess: true,
                message: "Auto Live to Finished is disabled.",
                errorMessage: null
            );

            return;
        }

        var durationMinutes = defaultMatchDurationMinutes <= 0
            ? 120
            : defaultMatchDurationMinutes;

        var finishTimeLimit = DateTime.Now.AddMinutes(-durationMinutes);

        var fixtures = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                x.Status == MatchStatus.Live &&
                x.MatchDateTime <= finishTimeLimit)
            .OrderBy(x => x.MatchDateTime)
            .ToListAsync();

        if (!fixtures.Any())
        {
            await WriteLogAsync(
                automationType: "FixtureStatus",
                entityType: "Fixture",
                entityId: null,
                actionName: "LiveToFinished",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: false,
                isSuccess: true,
                message: "No Live fixture found to change to Finished.",
                errorMessage: null
            );

            return;
        }

        foreach (var fixture in fixtures)
        {
            var beforeJson = JsonSerializer.Serialize(new
            {
                fixture.Id,
                fixture.TeamOneName,
                fixture.TeamTwoName,
                fixture.Status,
                fixture.MatchDateTime
            });

            var afterJson = JsonSerializer.Serialize(new
            {
                fixture.Id,
                NewStatus = MatchStatus.Finished.ToString()
            });

            if (guard.CanSuggest)
            {
                await CreateSuggestionIfNotExistsAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    suggestedAction: "LiveToFinished",
                    reason: $"{fixture.TeamOneName} vs {fixture.TeamTwoName} may be Finished because default match duration has passed.",
                    confidenceScore: 80,
                    beforeJson: beforeJson,
                    suggestedJson: afterJson
                );

                await WriteLogAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    actionName: "LiveToFinished",
                    executionMode: guard.ExecutionMode.ToString(),
                    beforeDataJson: beforeJson,
                    afterDataJson: afterJson,
                    isExecuted: false,
                    isSuccess: true,
                    message: "Live to Finished suggestion created.",
                    errorMessage: null
                );

                continue;
            }

            if (guard.CanExecute)
            {
                fixture.Status = MatchStatus.Finished;

                await WriteLogAsync(
                    automationType: "FixtureStatus",
                    entityType: "Fixture",
                    entityId: fixture.Id.ToString(),
                    actionName: "LiveToFinished",
                    executionMode: guard.ExecutionMode.ToString(),
                    beforeDataJson: beforeJson,
                    afterDataJson: afterJson,
                    isExecuted: true,
                    isSuccess: true,
                    message: $"{fixture.TeamOneName} vs {fixture.TeamTwoName} changed from Live to Finished.",
                    errorMessage: null
                );
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateSuggestionIfNotExistsAsync(
        string automationType,
        string entityType,
        string entityId,
        string suggestedAction,
        string reason,
        int confidenceScore,
        string beforeJson,
        string suggestedJson)
    {
        var exists = await _context.AutomationSuggestions
            .AnyAsync(x =>
                !x.IsReviewed &&
                x.AutomationType == automationType &&
                x.EntityType == entityType &&
                x.EntityId == entityId &&
                x.SuggestedAction == suggestedAction);

        if (exists)
        {
            return;
        }

        _context.AutomationSuggestions.Add(new AutomationSuggestion
        {
            AutomationType = automationType,
            EntityType = entityType,
            EntityId = entityId,
            SuggestedAction = suggestedAction,
            Reason = reason,
            ConfidenceScore = confidenceScore,
            BeforeDataJson = beforeJson,
            SuggestedDataJson = suggestedJson,
            IsReviewed = false,
            IsApproved = false,
            IsRejected = false,
            CreatedAt = DateTime.Now
        });
    }

    private async Task WriteLogAsync(
        string automationType,
        string entityType,
        string? entityId,
        string actionName,
        string executionMode,
        string? beforeDataJson,
        string? afterDataJson,
        bool isExecuted,
        bool isSuccess,
        string message,
        string? errorMessage)
    {
        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = automationType,
            EntityType = entityType,
            EntityId = entityId,
            ActionName = actionName,
            ExecutionMode = executionMode,
            BeforeDataJson = beforeDataJson,
            AfterDataJson = afterDataJson,
            IsExecuted = isExecuted,
            IsSuccess = isSuccess,
            Message = message,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}