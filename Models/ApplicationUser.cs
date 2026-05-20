using Microsoft.AspNetCore.Identity;

namespace FootballPredictionGame.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public bool IsActive { get; set; } = false;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public int TotalScore { get; set; } = 0;
    public int ExactPredictionCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
