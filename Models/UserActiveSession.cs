using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class UserActiveSession
{
    public long Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    public DateTime LoginAt { get; set; } = DateTime.Now;

    public DateTime LastSeenAt { get; set; } = DateTime.Now;

    public DateTime? LogoutAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}