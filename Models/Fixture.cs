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

    [Display(Name = "Group E")]
    GroupE = 5,

    [Display(Name = "Group F")]
    GroupF = 6,

    [Display(Name = "Group G")]
    GroupG = 7,

    [Display(Name = "Group H")]
    GroupH = 8,

    [Display(Name = "Group I")]
    GroupI = 9,

    [Display(Name = "Group J")]
    GroupJ = 10,

    [Display(Name = "Group K")]
    GroupK = 11,

    [Display(Name = "Group L")]
    GroupL = 12,

    [Display(Name = "Round of 32")]
    Roundof32 = 13,

    [Display(Name = "Round of 16")]
    Roundof16 = 14,

    [Display(Name = "Quarter-finals")]
    QuarterFinal = 15,

    [Display(Name = "Semi-finals")]
    SemiFinal = 16,

    [Display(Name = "Third place play-off")]
    ThirdPlacePlayOff = 17,

    [Display(Name = "Final")]
    Final = 18
}
