namespace FootballPredictionGame.Services;

public interface IResultProcessingService
{
    Task<ResultProcessingResult> ProcessAsync(
        int fixtureId,
        string? processedByUserId,
        string source);

    Task<ResultProcessingResult> UndoAsync(
        int fixtureId,
        string? processedByUserId,
        string source);
}