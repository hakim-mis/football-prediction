namespace FootballPredictionGame.Services;

public class ResultProcessingResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public int FixtureId { get; set; }

    public int TotalPredictionsProcessed { get; set; }

    public int ExactPredictionCount { get; set; }

    public int CorrectResultCount { get; set; }

    public int ZeroPointCount { get; set; }

    public string RedirectAction { get; set; } = "Index";

    public string RedirectController { get; set; } = "Fixtures";

    public object? RedirectRouteValues { get; set; }
}