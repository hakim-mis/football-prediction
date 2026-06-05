// Models/UserLoginHistory.cs
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class UserLoginHistory
{
    public long Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime LoginAt { get; set; } = DateTime.Now;

    public DateTime? LogoutAt { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    public bool IsSuccess { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }
}