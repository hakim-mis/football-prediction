using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FootballPredictionGame.ViewModels;

public class DashboardBannerViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? ButtonText { get; set; }

    public string? RedirectUrl { get; set; }

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public int DisplayOrder { get; set; }
}

public class DashboardBannerIndexViewModel
{
    public List<DashboardBannerViewModel> Banners { get; set; } = new();

    public int TotalBanners { get; set; }

    public int ActiveBanners { get; set; }

    public int InactiveBanners { get; set; }
}

public class DashboardBannerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    [Display(Name = "Banner Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [StringLength(50)]
    [Display(Name = "Button Text")]
    public string? ButtonText { get; set; }

    [StringLength(500)]
    [Display(Name = "Redirect URL")]
    public string? RedirectUrl { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Range(1, 999)]
    [Display(Name = "Priority")]
    public int Priority { get; set; } = 1;

    [Range(1, 999)]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 1;

    [Display(Name = "Banner Image")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImageUrl { get; set; }
}