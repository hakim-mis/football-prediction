// ViewModels/UserActiveSessionViewModel.cs
namespace FootballPredictionGame.ViewModels;

public class UserActiveSessionViewModel
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime LoginAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string OnlineForText
    {
        get
        {
            var duration = DateTime.Now - LoginAt;

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

    public string LastSeenText
    {
        get
        {
            var duration = DateTime.Now - LastSeenAt;

            if (duration.TotalSeconds < 60)
            {
                return "Just now";
            }

            if (duration.TotalMinutes < 60)
            {
                return $"{(int)duration.TotalMinutes} min ago";
            }

            return $"{(int)duration.TotalHours} hr ago";
        }
    }
}