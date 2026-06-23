using FootballPredictionGame.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels;

public class AdIndexViewModel
{
    public List<AdListItemViewModel> Ads { get; set; } = new();

    public int TotalAds { get; set; }

    public int ActiveAds { get; set; }

    public int TotalSlides { get; set; }

    public int TotalImpressions { get; set; }

    public int TotalSkips { get; set; }

    public int TotalClicks { get; set; }

    public int TotalCompletes { get; set; }
}

public class AdListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public int Priority { get; set; }

    public AdSelectionMode SelectionMode { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public int SlideCount { get; set; }

    public int ActiveSlideCount { get; set; }

    public int ImpressionCount { get; set; }

    public int SkipCount { get; set; }

    public int ClickCount { get; set; }

    public int CompleteCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    [Display(Name = "Ad Title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 0;

    [Range(0, 100)]
    [Display(Name = "Priority")]
    public int Priority { get; set; } = 0;

    [Display(Name = "Selection Mode")]
    public AdSelectionMode SelectionMode { get; set; } = AdSelectionMode.Ordered;

    [DataType(DataType.DateTime)]
    [Display(Name = "Start At")]
    public DateTime? StartAt { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "End At")]
    public DateTime? EndAt { get; set; }

    [Display(Name = "Show on Desktop")]
    public bool ShowOnDesktop { get; set; } = true;

    [Display(Name = "Show on Tablet")]
    public bool ShowOnTablet { get; set; } = true;

    [Display(Name = "Show on Mobile")]
    public bool ShowOnMobile { get; set; } = true;

    [Display(Name = "Show to Guests")]
    public bool ShowToGuests { get; set; } = true;

    [Display(Name = "Show to Users")]
    public bool ShowToUsers { get; set; } = true;

    [Display(Name = "Show to Admins")]
    public bool ShowToAdmins { get; set; } = false;

    [Display(Name = "Show on Dashboard")]
    public bool ShowOnDashboard { get; set; } = true;

    [Display(Name = "Show on Prediction Score")]
    public bool ShowOnPredictionScore { get; set; } = true;

    [Display(Name = "Show on Leaderboard")]
    public bool ShowOnLeaderboard { get; set; } = true;

    [Display(Name = "Show on Rules")]
    public bool ShowOnRules { get; set; } = false;

    [Display(Name = "Show on Login/Register")]
    public bool ShowOnLoginRegister { get; set; } = false;

    [Display(Name = "Show After Prediction Submit")]
    public bool ShowAfterPredictionSubmit { get; set; } = false;

    [Display(Name = "Show Only If User Has No Upcoming Prediction")]
    public bool ShowOnlyIfUserHasNoUpcomingPrediction { get; set; } = false;

    [MaxLength(80)]
    [Display(Name = "Default Button Text")]
    public string? ButtonText { get; set; }

    [MaxLength(800)]
    [Url]
    [Display(Name = "Default Button URL")]
    public string? ButtonUrl { get; set; }
}

public class AdSlideIndexViewModel
{
    public int AdId { get; set; }

    public string AdTitle { get; set; } = string.Empty;

    public bool AdIsActive { get; set; }

    public List<AdSlideListItemViewModel> Slides { get; set; } = new();
}

public class AdSlideListItemViewModel
{
    public int Id { get; set; }

    public int AdId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public string? AudioPath { get; set; }

    public string? ButtonText { get; set; }

    public string? ButtonUrl { get; set; }

    public int DurationSeconds { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdSlideFormViewModel
{
    public int Id { get; set; }

    public int AdId { get; set; }

    public string? AdTitle { get; set; }

    [Required]
    [MaxLength(150)]
    [Display(Name = "Slide Title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Image")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Audio / Voice")]
    public IFormFile? AudioFile { get; set; }

    public string? ExistingImagePath { get; set; }

    public string? ExistingAudioPath { get; set; }

    [Display(Name = "Remove Audio")]
    public bool RemoveAudio { get; set; }

    [MaxLength(80)]
    [Display(Name = "Button Text")]
    public string? ButtonText { get; set; }

    [MaxLength(800)]
    [Url]
    [Display(Name = "Button URL")]
    public string? ButtonUrl { get; set; }

    [Range(1, 60)]
    [Display(Name = "Duration Seconds")]
    public int DurationSeconds { get; set; } = 4;

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

/*
    Advanced Ads Analytics ViewModels
*/

public class AdAnalyticsViewModel
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int TotalAds { get; set; }

    public int TotalSlides { get; set; }

    public int TotalImpressions { get; set; }

    public int TotalSkips { get; set; }

    public int TotalClicks { get; set; }

    public int TotalCompletes { get; set; }

    public int TotalSoundEnabled { get; set; }

    public int TotalClosed { get; set; }

    public decimal ClickThroughRate { get; set; }

    public decimal SkipRate { get; set; }

    public decimal CompletionRate { get; set; }

    public List<AdAnalyticsItemViewModel> Items { get; set; } = new();

    public List<AdAnalyticsDeviceViewModel> DeviceItems { get; set; } = new();

    public List<AdAnalyticsPageViewModel> PageItems { get; set; } = new();

    public List<AdAnalyticsDailyViewModel> DailyItems { get; set; } = new();

    public List<AdAnalyticsEventViewModel> EventItems { get; set; } = new();
}

public class AdAnalyticsItemViewModel
{
    public int AdId { get; set; }

    public string AdTitle { get; set; } = string.Empty;

    public int ImpressionCount { get; set; }

    public int SkipCount { get; set; }

    public int ClickCount { get; set; }

    public int CompleteCount { get; set; }

    public int SoundEnabledCount { get; set; }

    public int CloseCount { get; set; }

    public decimal ClickThroughRate { get; set; }

    public decimal SkipRate { get; set; }

    public decimal CompletionRate { get; set; }
}

public class AdAnalyticsDeviceViewModel
{
    public string DeviceType { get; set; } = "Unknown";

    public int ImpressionCount { get; set; }

    public int SkipCount { get; set; }

    public int ClickCount { get; set; }

    public int CompleteCount { get; set; }

    public decimal ClickThroughRate { get; set; }

    public decimal SkipRate { get; set; }

    public decimal CompletionRate { get; set; }
}

public class AdAnalyticsPageViewModel
{
    public string PageName { get; set; } = "Unknown";

    public int ImpressionCount { get; set; }

    public int SkipCount { get; set; }

    public int ClickCount { get; set; }

    public int CompleteCount { get; set; }

    public decimal ClickThroughRate { get; set; }

    public decimal SkipRate { get; set; }

    public decimal CompletionRate { get; set; }
}

public class AdAnalyticsDailyViewModel
{
    public DateTime Date { get; set; }

    public int ImpressionCount { get; set; }

    public int SkipCount { get; set; }

    public int ClickCount { get; set; }

    public int CompleteCount { get; set; }

    public decimal ClickThroughRate { get; set; }

    public decimal SkipRate { get; set; }

    public decimal CompletionRate { get; set; }
}

public class AdAnalyticsEventViewModel
{
    public string EventType { get; set; } = string.Empty;

    public int Count { get; set; }
}