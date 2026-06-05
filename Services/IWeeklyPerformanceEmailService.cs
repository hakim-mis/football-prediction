using FootballPredictionGame.Models;

namespace FootballPredictionGame.Services;

public interface IWeeklyPerformanceEmailService
{
    Task SendAppreciationEmailAsync(
        ApplicationUser user,
        int weeklyPoint,
        int predictionCount,
        int exactPredictionCount,
        int missedPredictionCount,
        DateTime weekStart,
        DateTime weekEnd);

    Task SendImprovementEmailAsync(
        ApplicationUser user,
        int weeklyPoint,
        int predictionCount,
        int exactPredictionCount,
        int missedPredictionCount,
        DateTime weekStart,
        DateTime weekEnd);
}