namespace FootballPredictionGame.Models;

public class ResultProcessingLog
{
    public int Id { get; set; }

    public int FixtureId { get; set; }
    public Fixture Fixture { get; set; } = null!;

    public DateTime ProcessedAt { get; set; } = DateTime.Now;
    public string? ProcessedByUserId { get; set; }
    public int TotalPredictionsProcessed { get; set; }
}
