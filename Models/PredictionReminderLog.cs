using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class PredictionReminderLog
{
    public long Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int FixtureId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReminderType { get; set; } = string.Empty;
    // 24Hour / 1Hour

    [MaxLength(256)]
    public string? EmailTo { get; set; }

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ApplicationUser? User { get; set; }

    public Fixture? Fixture { get; set; }
}