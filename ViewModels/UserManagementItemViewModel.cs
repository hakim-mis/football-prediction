namespace FootballPredictionGame.ViewModels;

public class UserManagementItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; }
    public int TotalScore { get; set; }
    public int ExactPredictionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
