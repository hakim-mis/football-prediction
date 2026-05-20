using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class Fixture
{
    public int Id { get; set; }

    [Required]
    public string TeamOneName { get; set; } = string.Empty;
    public string? TeamOneFlagPath { get; set; }

    [Required]
    public string TeamTwoName { get; set; } = string.Empty;
    public string? TeamTwoFlagPath { get; set; }

    public FixtureStage Stage { get; set; } = FixtureStage.GroupA;

    public DateTime MatchDateTime { get; set; }

    public int? TeamOneActualGoal { get; set; }
    public int? TeamTwoActualGoal { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Upcoming;
    public bool IsPublished { get; set; } = true;
    public bool IsProcessed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}

public enum MatchStatus
{
    Upcoming = 1,
    Live = 2,
    Finished = 3
}


public enum FixtureStage
{
    [Display(Name = "Group A")]
    GroupA = 1,

    [Display(Name = "Group B")]
    GroupB = 2,

    [Display(Name = "Group C")]
    GroupC = 3,

    [Display(Name = "Group D")]
    GroupD = 4,

    [Display(Name = "Quarter Final")]
    QuarterFinal = 5,

    [Display(Name = "Semi Final")]
    SemiFinal = 6,

    [Display(Name = "Final")]
    Final = 7
}
