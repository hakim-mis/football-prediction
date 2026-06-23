using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class AdLog
{
    public int Id { get; set; }

    public int? AdId { get; set; }

    public Ad? Ad { get; set; }

    public int? AdSlideId { get; set; }

    public AdSlide? AdSlide { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(120)]
    public string? SessionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    // Impression, Skip, Complete, Click, SoundEnabled, Close

    [MaxLength(50)]
    public string? DeviceType { get; set; }
    // Desktop, Tablet, Mobile

    [MaxLength(100)]
    public string? PageName { get; set; }

    [MaxLength(800)]
    public string? PageUrl { get; set; }

    [MaxLength(80)]
    public string? IpAddress { get; set; }

    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    public string? ExtraDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}