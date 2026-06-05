using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FootballPredictionGame.Services.Automation;

public class PredictionReminderAutomationJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPredictionReminderEmailService _predictionReminderEmailService;

    public PredictionReminderAutomationJob(
        ApplicationDbContext context,
        IAutomationGuardService automationGuard,
        UserManager<ApplicationUser> userManager,
        IPredictionReminderEmailService predictionReminderEmailService)
    {
        _context = context;
        _automationGuard = automationGuard;
        _userManager = userManager;
        _predictionReminderEmailService = predictionReminderEmailService;
    }

    public async Task RunAsync()
    {
        var guard = await _automationGuard.CanRunAsync(AutomationFeature.ReminderAutomation);

        if (!guard.CanRun)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "PredictionReminderAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: false,
                isSuccess: true,
                message: guard.Message,
                errorMessage: null
            );

            return;
        }

        var settings = guard.Settings ?? await _automationGuard.GetSettingsAsync();

        try
        {
            if (settings.Reminder24HourEnabled)
            {
                await ProcessReminderTypeAsync(
                    guard: guard,
                    reminderType: "24Hour",
                    windowStart: DateTime.Now.AddHours(23).AddMinutes(45),
                    windowEnd: DateTime.Now.AddHours(24).AddMinutes(15)
                );
            }

            if (settings.Reminder1HourEnabled)
            {
                await ProcessReminderTypeAsync(
                    guard: guard,
                    reminderType: "1Hour",
                    windowStart: DateTime.Now.AddMinutes(45),
                    windowEnd: DateTime.Now.AddMinutes(75)
                );
            }
        }
        catch (Exception ex)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "PredictionReminderAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: true,
                isSuccess: false,
                message: "Prediction reminder automation failed.",
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private async Task ProcessReminderTypeAsync(
        AutomationGuardResult guard,
        string reminderType,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var fixtures = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                x.Status == MatchStatus.Upcoming &&
                x.MatchDateTime >= windowStart &&
                x.MatchDateTime <= windowEnd)
            .OrderBy(x => x.MatchDateTime)
            .ToListAsync();

        if (!fixtures.Any())
        {
            await WriteLogAsync(
                entityId: null,
                actionName: $"Send{reminderType}Reminder",
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: false,
                isSuccess: true,
                message: $"No fixture found for {reminderType} reminder window.",
                errorMessage: null
            );

            return;
        }

        var users = await _userManager.GetUsersInRoleAsync("User");
        var activeUsers = users
            .Where(x => x.IsActive && x.EmailConfirmed && !string.IsNullOrWhiteSpace(x.Email))
            .ToList();

        foreach (var fixture in fixtures)
        {
            var predictedUserIds = await _context.Predictions
                .Where(x => x.FixtureId == fixture.Id)
                .Select(x => x.UserId)
                .ToListAsync();

            var notPredictedUsers = activeUsers
                .Where(x => !predictedUserIds.Contains(x.Id))
                .ToList();

            foreach (var user in notPredictedUsers)
            {
                var alreadyLogged = await _context.PredictionReminderLogs
                    .AnyAsync(x =>
                        x.UserId == user.Id &&
                        x.FixtureId == fixture.Id &&
                        x.ReminderType == reminderType);

                if (alreadyLogged)
                {
                    continue;
                }

                var beforeJson = JsonSerializer.Serialize(new
                {
                    fixture.Id,
                    fixture.TeamOneName,
                    fixture.TeamTwoName,
                    fixture.MatchDateTime,
                    UserId = user.Id,
                    user.Email,
                    ReminderType = reminderType
                });

                if (guard.CanSuggest)
                {
                    await CreateReminderSuggestionIfNotExistsAsync(
                        fixture: fixture,
                        user: user,
                        reminderType: reminderType,
                        beforeJson: beforeJson
                    );

                    _context.PredictionReminderLogs.Add(new PredictionReminderLog
                    {
                        UserId = user.Id,
                        FixtureId = fixture.Id,
                        ReminderType = reminderType,
                        EmailTo = user.Email,
                        IsSent = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    await WriteLogAsync(
                        entityId: fixture.Id.ToString(),
                        actionName: $"Suggest{reminderType}Reminder",
                        executionMode: guard.ExecutionMode.ToString(),
                        isExecuted: false,
                        isSuccess: true,
                        message: $"{reminderType} reminder suggestion created for {user.Email}.",
                        errorMessage: null
                    );

                    continue;
                }

                if (guard.CanExecute)
                {
                    try
                    {
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

                        await WriteLogAsync(
                            entityId: fixture.Id.ToString(),
                            actionName: $"Send{reminderType}Reminder",
                            executionMode: guard.ExecutionMode.ToString(),
                            isExecuted: true,
                            isSuccess: true,
                            message: $"{reminderType} reminder email sent to {user.Email}.",
                            errorMessage: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _context.PredictionReminderLogs.Add(new PredictionReminderLog
                        {
                            UserId = user.Id,
                            FixtureId = fixture.Id,
                            ReminderType = reminderType,
                            EmailTo = user.Email,
                            IsSent = false,
                            ErrorMessage = ex.Message,
                            CreatedAt = DateTime.Now
                        });

                        await _context.SaveChangesAsync();

                        await WriteLogAsync(
                            entityId: fixture.Id.ToString(),
                            actionName: $"Send{reminderType}Reminder",
                            executionMode: guard.ExecutionMode.ToString(),
                            isExecuted: true,
                            isSuccess: false,
                            message: $"{reminderType} reminder email failed for {user.Email}.",
                            errorMessage: ex.Message
                        );
                    }
                }
            }
        }
    }

    private async Task CreateReminderSuggestionIfNotExistsAsync(
        Fixture fixture,
        ApplicationUser user,
        string reminderType,
        string beforeJson)
    {
        var entityId = $"{fixture.Id}:{user.Id}:{reminderType}";

        var exists = await _context.AutomationSuggestions
            .AnyAsync(x =>
                !x.IsReviewed &&
                x.AutomationType == "PredictionReminder" &&
                x.EntityType == "Email" &&
                x.EntityId == entityId &&
                x.SuggestedAction == $"Send{reminderType}Reminder");

        if (exists)
        {
            return;
        }

        _context.AutomationSuggestions.Add(new AutomationSuggestion
        {
            AutomationType = "PredictionReminder",
            EntityType = "Email",
            EntityId = entityId,
            SuggestedAction = $"Send{reminderType}Reminder",
            Reason = $"{user.FullName} has not predicted {fixture.TeamOneName} vs {fixture.TeamTwoName}.",
            ConfidenceScore = 100,
            BeforeDataJson = beforeJson,
            SuggestedDataJson = JsonSerializer.Serialize(new
            {
                FixtureId = fixture.Id,
                UserId = user.Id,
                ReminderType = reminderType,
                EmailTo = user.Email
            }),
            IsReviewed = false,
            IsApproved = false,
            IsRejected = false,
            CreatedAt = DateTime.Now
        });
    }

    private async Task WriteLogAsync(
        string? entityId,
        string actionName,
        string executionMode,
        bool isExecuted,
        bool isSuccess,
        string message,
        string? errorMessage)
    {
        _context.AutomationLogs.Add(new AutomationLog
        {
            AutomationType = "PredictionReminder",
            EntityType = "Email",
            EntityId = entityId,
            ActionName = actionName,
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