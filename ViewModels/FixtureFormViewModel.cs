using FootballPredictionGame.Models;
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels;

public class FixtureFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Fixture Segment")]
    public FixtureStage Stage { get; set; } = FixtureStage.GroupA;

    [Required, StringLength(100)]
    [Display(Name = "Team One Name")]
    public string TeamOneName { get; set; } = string.Empty;

    [Display(Name = "Team One Flag")]
    public IFormFile? TeamOneFlag { get; set; }
    public string? ExistingTeamOneFlagPath { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Team Two Name")]
    public string TeamTwoName { get; set; } = string.Empty;

    [Display(Name = "Team Two Flag")]
    public IFormFile? TeamTwoFlag { get; set; }
    public string? ExistingTeamTwoFlagPath { get; set; }

    [Required]
    [Display(Name = "Match Date and Time")]
    public DateTime MatchDateTime { get; set; } = DateTime.Now.AddHours(1);

    [Range(0, 99)]
    [Display(Name = "Team One Actual Goal")]
    public int? TeamOneActualGoal { get; set; }

    [Range(0, 99)]
    [Display(Name = "Team Two Actual Goal")]
    public int? TeamTwoActualGoal { get; set; }

    [Display(Name = "Match Status")]
    public MatchStatus Status { get; set; } = MatchStatus.Upcoming;

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;

    public bool IsProcessed { get; set; }
}
