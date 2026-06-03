namespace FootballPredictionGame.ViewModels
{
    public class FixturePredictionDetailViewModel
    {
        public int FixtureId { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateTime MatchDateTime { get; set; }

        public string TeamOneName { get; set; } = string.Empty;
        public string TeamOneFlagPath { get; set; } = string.Empty;

        public string TeamTwoName { get; set; } = string.Empty;
        public string TeamTwoFlagPath { get; set; } = string.Empty;

        public int? TeamOneActualGoal { get; set; }
        public int? TeamTwoActualGoal { get; set; }

        public List<FixturePredictionUserDetailViewModel> Predictions { get; set; } = new();
    }

    public class FixturePredictionUserDetailViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty;
        public string RankText { get; set; } = string.Empty;

        public int TeamOnePredictedGoal { get; set; }
        public int TeamTwoPredictedGoal { get; set; }

        public int EarnedPoint { get; set; }
        public int TotalScore { get; set; }
    }
}