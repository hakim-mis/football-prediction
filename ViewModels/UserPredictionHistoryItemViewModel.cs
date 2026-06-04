using FootballPredictionGame.Models;

public class UserPredictionHistoryItemViewModel
{
    public int FixtureId { get; set; }

    public string StageName { get; set; } = string.Empty;
    public MatchStatus Status { get; set; }

    public DateTime MatchDateTime { get; set; }

    public string TeamOneName { get; set; } = string.Empty;
    public string TeamOneFlagPath { get; set; } = "/img/default-flag.svg";

    public string TeamTwoName { get; set; } = string.Empty;
    public string TeamTwoFlagPath { get; set; } = "/img/default-flag.svg";

    public int? TeamOneActualGoal { get; set; }
    public int? TeamTwoActualGoal { get; set; }

    public bool HasPrediction { get; set; }

    public int? TeamOnePredictedGoal { get; set; }
    public int? TeamTwoPredictedGoal { get; set; }

    public int EarnedPoint { get; set; }
}