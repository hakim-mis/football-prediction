using DocumentFormat.OpenXml.Presentation;
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class Ad
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public int DisplayOrder { get; set; } = 0;

    [Range(0, 100)]
    public int Priority { get; set; } = 0;

    public AdSelectionMode SelectionMode { get; set; } = AdSelectionMode.Ordered;

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    // Platform targeting
    public bool ShowOnDesktop { get; set; } = true;

    public bool ShowOnTablet { get; set; } = true;

    public bool ShowOnMobile { get; set; } = true;

    // Audience targeting
    public bool ShowToGuests { get; set; } = true;

    public bool ShowToUsers { get; set; } = true;

    public bool ShowToAdmins { get; set; } = false;

    // Page targeting
    public bool ShowOnDashboard { get; set; } = true;

    public bool ShowOnPredictionScore { get; set; } = true;

    public bool ShowOnLeaderboard { get; set; } = true;

    public bool ShowOnRules { get; set; } = false;

    public bool ShowOnLoginRegister { get; set; } = false;

    // Smart rules for future phase
    public bool ShowAfterPredictionSubmit { get; set; } = false;

    public bool ShowOnlyIfUserHasNoUpcomingPrediction { get; set; } = false;

    [MaxLength(80)]
    public string? ButtonText { get; set; }

    [MaxLength(800)]
    public string? ButtonUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }

    public ICollection<AdSlide> Slides { get; set; } = new List<AdSlide>();
}