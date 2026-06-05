using System.Text.Json;
using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services.Automation;

public class ResultProcessingAutomationJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;
    private readonly IResultProcessingService _resultProcessingService;

    public ResultProcessingAutomationJob(
        ApplicationDbContext context,
        IAutomationGuardService automationGuard,
        IResultProcessingService resultProcessingService)
    {
        _context = context;
        _automationGuard = automationGuard;
        _resultProcessingService = resultProcessingService;
    }

    public async Task RunAsync()
    {
        var guard = await _automationGuard.CanRunAsync(AutomationFeature.ResultProcessing);

        if (!guard.CanRun)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "ResultProcessingAutomation",
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
            var fixtures = await _context.Fixtures
                .Where(x =>
                    x.IsPublished &&
                    x.Status == MatchStatus.Finished &&
                    !x.IsProcessed &&
                    x.TeamOneActualGoal.HasValue &&
                    x.TeamTwoActualGoal.HasValue)
                .OrderBy(x => x.MatchDateTime)
                .ToListAsync();

            if (!fixtures.Any())
            {
                await WriteLogAsync(
                    entityId: null,
                    actionName: "ProcessResult",
                    executionMode: guard.ExecutionMode.ToString(),
                    beforeDataJson: null,
                    afterDataJson: null,
                    isExecuted: false,
                    isSuccess: true,
                    message: "No fixture found for result processing.",
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
                    fixture.TeamOneActualGoal,
                    fixture.TeamTwoActualGoal,
                    fixture.Status,
                    fixture.IsProcessed
                });

                var afterJson = JsonSerializer.Serialize(new
                {
                    fixture.Id,
                    NewIsProcessed = true,
                    Action = "ProcessResult"
                });

                if (guard.CanSuggest)
                {
                    await CreateSuggestionIfNotExistsAsync(
                        fixture: fixture,
                        beforeJson: beforeJson,
                        suggestedJson: afterJson
                    );

                    await WriteLogAsync(
                        entityId: fixture.Id.ToString(),
                        actionName: "ProcessResult",
                        executionMode: guard.ExecutionMode.ToString(),
                        beforeDataJson: beforeJson,
                        afterDataJson: afterJson,
                        isExecuted: false,
                        isSuccess: true,
                        message: $"Process result suggestion created for {fixture.TeamOneName} vs {fixture.TeamTwoName}.",
                        errorMessage: null
                    );

                    continue;
                }

                if (guard.CanExecute)
                {
                    var result = await _resultProcessingService.ProcessAsync(
                        fixtureId: fixture.Id,
                        processedByUserId: "AUTO",
                        source: "Automation"
                    );

                    await WriteLogAsync(
                        entityId: fixture.Id.ToString(),
                        actionName: "ProcessResult",
                        executionMode: guard.ExecutionMode.ToString(),
                        beforeDataJson: beforeJson,
                        afterDataJson: afterJson,
                        isExecuted: result.Success,
                        isSuccess: result.Success,
                        message: result.Success ? result.Message : "Auto result processing failed.",
                        errorMessage: result.ErrorMessage
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "ResultProcessingAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                beforeDataJson: null,
                afterDataJson: null,
                isExecuted: true,
                isSuccess: false,
                message: "Result processing automation failed.",
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private async Task CreateSuggestionIfNotExistsAsync(
        Fixture fixture,
        string beforeJson,
        string suggestedJson)
    {
        var fixtureId = fixture.Id.ToString();

        var exists = await _context.AutomationSuggestions
            .AnyAsync(x =>
                !x.IsReviewed &&
                x.AutomationType == "ResultProcessing" &&
                x.EntityType == "Fixture" &&
                x.EntityId == fixtureId &&
                x.SuggestedAction == "ProcessResult");

        if (exists)
        {
            return;
        }

        _context.AutomationSuggestions.Add(new AutomationSuggestion
        {
            AutomationType = "ResultProcessing",
            EntityType = "Fixture",
            EntityId = fixtureId,
            SuggestedAction = "ProcessResult",
            Reason = $"{fixture.TeamOneName} vs {fixture.TeamTwoName} has actual score and is ready for point processing.",
            ConfidenceScore = 100,
            BeforeDataJson = beforeJson,
            SuggestedDataJson = suggestedJson,
            IsReviewed = false,
            IsApproved = false,
            IsRejected = false,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    private async Task WriteLogAsync(
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
            AutomationType = "ResultProcessing",
            EntityType = "Fixture",
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