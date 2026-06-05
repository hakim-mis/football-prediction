using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class WeeklyPerformanceEmailLog
{
    public long Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? EmailTo { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmailType { get; set; } = string.Empty;
    // Appreciation / Improvement

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public int WeeklyPoint { get; set; }

    public int WeeklyPredictionCount { get; set; }

    public int WeeklyExactPredictionCount { get; set; }

    public int MissedPredictionCount { get; set; }

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ApplicationUser? User { get; set; }
}