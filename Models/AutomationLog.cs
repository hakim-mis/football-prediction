// Models/AutomationLog.cs
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class AutomationLog
{
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string AutomationType { get; set; } = string.Empty;
    // FixtureStatus, ScoreSync, ReminderEmail, WeeklyEmail, AiUserApproval, LoginTracking

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    // Fixture, Prediction, User, Email, Session

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [Required]
    [MaxLength(150)]
    public string ActionName { get; set; } = string.Empty;
    // UpcomingToLive, SendReminder24Hour, AutoApproveUser

    [Required]
    [MaxLength(50)]
    public string ExecutionMode { get; set; } = string.Empty;
    // Manual, SuggestOnly, AutoExecute

    public string? BeforeDataJson { get; set; }
    public string? AfterDataJson { get; set; }

    public bool IsExecuted { get; set; }
    public bool IsSuccess { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}