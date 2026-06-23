namespace FootballPredictionGame.ViewModels;
public class UserPredictionHistoryViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string PhotoPath { get; set; } = "/img/default-avatar.svg";

    public string RankText { get; set; } = "No rank";
    public int TotalScore { get; set; }
    public int ExactPredictionCount { get; set; }
    public int WinMatchPredictionCount { get; set; }

    public List<UserPredictionHistoryItemViewModel> Items { get; set; } = new();
}