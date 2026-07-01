using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class DashboardBanner
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Subtitle { get; set; }

    [Required, MaxLength(300)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ButtonText { get; set; }

    [MaxLength(500)]
    public string? RedirectUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public int Priority { get; set; } = 1;

    public int DisplayOrder { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}