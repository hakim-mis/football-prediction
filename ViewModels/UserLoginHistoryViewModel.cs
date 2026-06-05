// ViewModels/UserLoginHistoryViewModel.cs
namespace FootballPredictionGame.ViewModels;

public class UserLoginHistoryViewModel
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime LoginAt { get; set; }

    public DateTime? LogoutAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? SessionId { get; set; }

    public bool IsSuccess { get; set; }

    public string? FailureReason { get; set; }

    public string DurationText
    {
        get
        {
            var end = LogoutAt ?? DateTime.Now;
            var duration = end - LoginAt;

            if (duration.TotalMinutes < 1)
            {
                return "Less than 1 min";
            }

            if (duration.TotalHours < 1)
            {
                return $"{(int)duration.TotalMinutes} min";
            }

            return $"{(int)duration.TotalHours} hr {(int)duration.Minutes} min";
        }
    }
}