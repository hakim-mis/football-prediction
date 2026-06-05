using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FootballPredictionGame.Services.Automation;

public class WeeklyPerformanceEmailAutomationJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAutomationGuardService _automationGuard;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWeeklyPerformanceEmailService _weeklyPerformanceEmailService;

    public WeeklyPerformanceEmailAutomationJob(
        ApplicationDbContext context,
        IAutomationGuardService automationGuard,
        UserManager<ApplicationUser> userManager,
        IWeeklyPerformanceEmailService weeklyPerformanceEmailService)
    {
        _context = context;
        _automationGuard = automationGuard;
        _userManager = userManager;
        _weeklyPerformanceEmailService = weeklyPerformanceEmailService;
    }

    public async Task RunAsync()
    {
        var guard = await _automationGuard.CanRunAsync(AutomationFeature.WeeklyEmailAutomation);

        if (!guard.CanRun)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "WeeklyPerformanceEmailAutomation",
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
            if (!IsScheduledTime(settings))
            {
                await WriteLogAsync(
                    entityId: null,
                    actionName: "WeeklyPerformanceEmailAutomation",
                    executionMode: guard.ExecutionMode.ToString(),
                    isExecuted: false,
                    isSuccess: true,
                    message: "Weekly email automation skipped because current time is outside configured schedule.",
                    errorMessage: null
                );

                return;
            }

            var today = DateTime.Today;
            var weekEnd = today;
            var weekStart = today.AddDays(-7);

            var players = await _userManager.GetUsersInRoleAsync("User");

            var activeUsers = players
                .Where(x => x.IsActive && x.EmailConfirmed && !string.IsNullOrWhiteSpace(x.Email))
                .ToList();

            foreach (var user in activeUsers)
            {
                var stats = await BuildWeeklyStatsAsync(user.Id, weekStart, weekEnd);

                if (settings.WeeklyAppreciationEmailEnabled &&
                    stats.WeeklyPoint >= settings.WeeklyGoodPerformancePointThreshold)
                {
                    await HandleEmailAsync(
                        guard,
                        user,
                        stats,
                        weekStart,
                        weekEnd,
                        emailType: "Appreciation"
                    );
                }

                if (settings.WeeklyImprovementEmailEnabled &&
                    (stats.WeeklyPoint <= settings.WeeklyPoorPerformancePointThreshold ||
                     stats.MissedPredictionCount >= settings.WeeklyMissedPredictionThreshold))
                {
                    await HandleEmailAsync(
                        guard,
                        user,
                        stats,
                        weekStart,
                        weekEnd,
                        emailType: "Improvement"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await WriteLogAsync(
                entityId: null,
                actionName: "WeeklyPerformanceEmailAutomation",
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: true,
                isSuccess: false,
                message: "Weekly performance email automation failed.",
                errorMessage: ex.Message
            );

            throw;
        }
    }

    private bool IsScheduledTime(AutomationSettings settings)
    {
        var now = DateTime.Now;

        if (now.DayOfWeek != settings.WeeklyEmailSendDay)
        {
            return false;
        }

        var scheduledTime = settings.WeeklyEmailSendTime;
        var currentTime = now.TimeOfDay;

        var windowStart = scheduledTime;
        var windowEnd = scheduledTime.Add(TimeSpan.FromMinutes(30));

        return currentTime >= windowStart && currentTime <= windowEnd;
    }

    private async Task<WeeklyUserPerformanceStats> BuildWeeklyStatsAsync(
        string userId,
        DateTime weekStart,
        DateTime weekEnd)
    {
        var weekEndExclusive = weekEnd.AddDays(1);

        var weeklyPredictions = await _context.Predictions
            .Include(x => x.Fixture)
            .Where(x =>
                x.UserId == userId &&
                x.IsProcessed &&
                x.Fixture.MatchDateTime >= weekStart &&
                x.Fixture.MatchDateTime < weekEndExclusive)
            .ToListAsync();

        var finishedPublishedFixtureIds = await _context.Fixtures
            .Where(x =>
                x.IsPublished &&
                x.Status == MatchStatus.Finished &&
                x.MatchDateTime >= weekStart &&
                x.MatchDateTime < weekEndExclusive)
            .Select(x => x.Id)
            .ToListAsync();

        var predictedFixtureIds = weeklyPredictions
            .Select(x => x.FixtureId)
            .Distinct()
            .ToList();

        var missedPredictionCount = finishedPublishedFixtureIds
            .Count(x => !predictedFixtureIds.Contains(x));

        return new WeeklyUserPerformanceStats
        {
            WeeklyPoint = weeklyPredictions.Sum(x => x.EarnedPoint),
            WeeklyPredictionCount = weeklyPredictions.Count,
            WeeklyExactPredictionCount = weeklyPredictions.Count(x => x.EarnedPoint == 3),
            MissedPredictionCount = missedPredictionCount
        };
    }

    private async Task HandleEmailAsync(
        AutomationGuardResult guard,
        ApplicationUser user,
        WeeklyUserPerformanceStats stats,
        DateTime weekStart,
        DateTime weekEnd,
        string emailType)
    {
        var alreadyLogged = await _context.WeeklyPerformanceEmailLogs
            .AnyAsync(x =>
                x.UserId == user.Id &&
                x.EmailType == emailType &&
                x.WeekStartDate == weekStart &&
                x.WeekEndDate == weekEnd);

        if (alreadyLogged)
        {
            return;
        }

        var entityId = $"{user.Id}:{emailType}:{weekStart:yyyyMMdd}:{weekEnd:yyyyMMdd}";

        var beforeJson = JsonSerializer.Serialize(new
        {
            user.Id,
            user.Email,
            EmailType = emailType,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            stats.WeeklyPoint,
            stats.WeeklyPredictionCount,
            stats.WeeklyExactPredictionCount,
            stats.MissedPredictionCount
        });

        if (guard.CanSuggest)
        {
            await CreateSuggestionIfNotExistsAsync(
                entityId,
                user,
                stats,
                weekStart,
                weekEnd,
                emailType,
                beforeJson
            );

            _context.WeeklyPerformanceEmailLogs.Add(new WeeklyPerformanceEmailLog
            {
                UserId = user.Id,
                EmailTo = user.Email,
                EmailType = emailType,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                WeeklyPoint = stats.WeeklyPoint,
                WeeklyPredictionCount = stats.WeeklyPredictionCount,
                WeeklyExactPredictionCount = stats.WeeklyExactPredictionCount,
                MissedPredictionCount = stats.MissedPredictionCount,
                IsSent = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await WriteLogAsync(
                entityId: user.Id,
                actionName: $"SuggestWeekly{emailType}Email",
                executionMode: guard.ExecutionMode.ToString(),
                isExecuted: false,
                isSuccess: true,
                message: $"Weekly {emailType} email suggestion created for {user.Email}.",
                errorMessage: null
            );

            return;
        }

        if (guard.CanExecute)
        {
            try
            {
                if (emailType == "Appreciation")
                {
                    await _weeklyPerformanceEmailService.SendAppreciationEmailAsync(
                        user,
                        stats.WeeklyPoint,
                        stats.WeeklyPredictionCount,
                        stats.WeeklyExactPredictionCount,
                        stats.MissedPredictionCount,
                        weekStart,
                        weekEnd
                    );
                }
                else
                {
                    await _weeklyPerformanceEmailService.SendImprovementEmailAsync(
                        user,
                        stats.WeeklyPoint,
                        stats.WeeklyPredictionCount,
                        stats.WeeklyExactPredictionCount,
                        stats.MissedPredictionCount,
                        weekStart,
                        weekEnd
                    );
                }

                _context.WeeklyPerformanceEmailLogs.Add(new WeeklyPerformanceEmailLog
                {
                    UserId = user.Id,
                    EmailTo = user.Email,
                    EmailType = emailType,
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    WeeklyPoint = stats.WeeklyPoint,
                    WeeklyPredictionCount = stats.WeeklyPredictionCount,
                    WeeklyExactPredictionCount = stats.WeeklyExactPredictionCount,
                    MissedPredictionCount = stats.MissedPredictionCount,
                    IsSent = true,
                    SentAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                await WriteLogAsync(
                    entityId: user.Id,
                    actionName: $"SendWeekly{emailType}Email",
                    executionMode: guard.ExecutionMode.ToString(),
                    isExecuted: true,
                    isSuccess: true,
                    message: $"Weekly {emailType} email sent to {user.Email}.",
                    errorMessage: null
                );
            }
            catch (Exception ex)
            {
                _context.WeeklyPerformanceEmailLogs.Add(new WeeklyPerformanceEmailLog
                {
                    UserId = user.Id,
                    EmailTo = user.Email,
                    EmailType = emailType,
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    WeeklyPoint = stats.WeeklyPoint,
                    WeeklyPredictionCount = stats.WeeklyPredictionCount,
                    WeeklyExactPredictionCount = stats.WeeklyExactPredictionCount,
                    MissedPredictionCount = stats.MissedPredictionCount,
                    IsSent = false,
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                await WriteLogAsync(
                    entityId: user.Id,
                    actionName: $"SendWeekly{emailType}Email",
                    executionMode: guard.ExecutionMode.ToString(),
                    isExecuted: true,
                    isSuccess: false,
                    message: $"Weekly {emailType} email failed for {user.Email}.",
                    errorMessage: ex.Message
                );
            }
        }
    }

    private async Task CreateSuggestionIfNotExistsAsync(
        string entityId,
        ApplicationUser user,
        WeeklyUserPerformanceStats stats,
        DateTime weekStart,
        DateTime weekEnd,
        string emailType,
        string beforeJson)
    {
        var suggestedAction = $"SendWeekly{emailType}Email";

        var exists = await _context.AutomationSuggestions
            .AnyAsync(x =>
                !x.IsReviewed &&
                x.AutomationType == "WeeklyPerformanceEmail" &&
                x.EntityType == "Email" &&
                x.EntityId == entityId &&
                x.SuggestedAction == suggestedAction);

        if (exists)
        {
            return;
        }

        var reason = emailType == "Appreciation"
            ? $"{user.FullName} earned {stats.WeeklyPoint} points this week and qualifies for appreciation email."
            : $"{user.FullName} needs improvement email. Weekly points: {stats.WeeklyPoint}, missed predictions: {stats.MissedPredictionCount}.";

        _context.AutomationSuggestions.Add(new AutomationSuggestion
        {
            AutomationType = "WeeklyPerformanceEmail",
            EntityType = "Email",
            EntityId = entityId,
            SuggestedAction = suggestedAction,
            Reason = reason,
            ConfidenceScore = 100,
            BeforeDataJson = beforeJson,
            SuggestedDataJson = JsonSerializer.Serialize(new
            {
                UserId = user.Id,
                EmailTo = user.Email,
                EmailType = emailType,
                WeekStart = weekStart,
                WeekEnd = weekEnd
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
            AutomationType = "WeeklyPerformanceEmail",
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

    private class WeeklyUserPerformanceStats
    {
        public int WeeklyPoint { get; set; }
        public int WeeklyPredictionCount { get; set; }
        public int WeeklyExactPredictionCount { get; set; }
        public int MissedPredictionCount { get; set; }
    }
}