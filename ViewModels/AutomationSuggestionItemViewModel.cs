namespace FootballPredictionGame.ViewModels;

public class AutomationSuggestionItemViewModel
{
    public long Id { get; set; }

    public string AutomationType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string SuggestedAction { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public int ConfidenceScore { get; set; }

    public bool IsReviewed { get; set; }

    public bool IsApproved { get; set; }

    public bool IsRejected { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string StatusText
    {
        get
        {
            if (!IsReviewed)
            {
                return "Pending";
            }

            if (IsApproved)
            {
                return "Executed";
            }

            if (IsRejected)
            {
                return "Rejected";
            }

            return "Reviewed";
        }
    }
}