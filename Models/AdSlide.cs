using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class AdSlide
{
    public int Id { get; set; }

    public int AdId { get; set; }

    public Ad Ad { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AudioPath { get; set; }

    [MaxLength(80)]
    public string? ButtonText { get; set; }

    [MaxLength(800)]
    public string? ButtonUrl { get; set; }

    [Range(1, 60)]
    public int DurationSeconds { get; set; } = 4;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}