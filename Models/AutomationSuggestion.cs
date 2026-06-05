// Models/AutomationSuggestion.cs
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class AutomationSuggestion
{
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string AutomationType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [Required]
    [MaxLength(150)]
    public string SuggestedAction { get; set; } = string.Empty;

    public string? Reason { get; set; }

    [Range(0, 100)]
    public int ConfidenceScore { get; set; }

    public string? BeforeDataJson { get; set; }
    public string? SuggestedDataJson { get; set; }

    public bool IsReviewed { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }

    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}