using FootballPredictionGame.Models;

namespace FootballPredictionGame.Services;

public interface IPredictionReminderEmailService
{
    Task SendPredictionReminderAsync(
        ApplicationUser user,
        Fixture fixture,
        string reminderType);
}