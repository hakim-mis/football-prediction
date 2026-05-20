using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class Prediction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int FixtureId { get; set; }
    public Fixture Fixture { get; set; } = null!;

    public int TeamOnePredictedGoal { get; set; }
    public int TeamTwoPredictedGoal { get; set; }

    public int EarnedPoint { get; set; } = 0;
    public bool IsProcessed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
