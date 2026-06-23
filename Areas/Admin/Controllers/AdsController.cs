using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdsController : Controller
{
    private const long MaxImageSizeBytes = 1 * 1024 * 1024;
    private const long MaxAudioSizeBytes = 2 * 1024 * 1024;

    private static readonly string[] AllowedImageExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly string[] AllowedAudioExtensions =
    {
        ".mp3"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public AdsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ads = await _context.Ads
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new AdListItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                IsActive = x.IsActive,
                DisplayOrder = x.DisplayOrder,
                Priority = x.Priority,
                SelectionMode = x.SelectionMode,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                SlideCount = x.Slides.Count(s => !s.IsDeleted),
                ActiveSlideCount = x.Slides.Count(s => !s.IsDeleted && s.IsActive),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var adIds = ads.Select(x => x.Id).ToList();

        var metricRows = await _context.AdLogs
            .Where(x => x.AdId.HasValue && adIds.Contains(x.AdId.Value))
            .GroupBy(x => new
            {
                AdId = x.AdId!.Value,
                x.EventType
            })
            .Select(g => new
            {
                g.Key.AdId,
                g.Key.EventType,
                Count = g.Count()
            })
            .ToListAsync();

        var metricMap = metricRows.ToDictionary(
            x => $"{x.AdId}|{x.EventType}",
            x => x.Count);

        foreach (var ad in ads)
        {
            ad.ImpressionCount = GetMetric(metricMap, ad.Id, "Impression");
            ad.SkipCount = GetMetric(metricMap, ad.Id, "Skip");
            ad.ClickCount = GetMetric(metricMap, ad.Id, "Click");
            ad.CompleteCount = GetMetric(metricMap, ad.Id, "Complete");
        }

        var model = new AdIndexViewModel
        {
            Ads = ads,
            TotalAds = ads.Count,
            ActiveAds = ads.Count(x => x.IsActive),
            TotalSlides = ads.Sum(x => x.SlideCount),
            TotalImpressions = ads.Sum(x => x.ImpressionCount),
            TotalSkips = ads.Sum(x => x.SkipCount),
            TotalClicks = ads.Sum(x => x.ClickCount),
            TotalCompletes = ads.Sum(x => x.CompleteCount)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new AdFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Priority = 0,
            SelectionMode = AdSelectionMode.Ordered,

            ShowOnDesktop = true,
            ShowOnTablet = true,
            ShowOnMobile = true,

            ShowToGuests = true,
            ShowToUsers = true,
            ShowToAdmins = false,

            ShowOnDashboard = true,
            ShowOnPredictionScore = true,
            ShowOnLeaderboard = true,
            ShowOnRules = false,
            ShowOnLoginRegister = false,

            ShowAfterPredictionSubmit = false,
            ShowOnlyIfUserHasNoUpcomingPrediction = false
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdFormViewModel model)
    {
        ValidateAdDateRange(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);

        var ad = new Ad
        {
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),

            IsActive = model.IsActive,
            IsDeleted = false,

            DisplayOrder = model.DisplayOrder,
            Priority = model.Priority,
            SelectionMode = model.SelectionMode,

            StartAt = model.StartAt,
            EndAt = model.EndAt,

            ShowOnDesktop = model.ShowOnDesktop,
            ShowOnTablet = model.ShowOnTablet,
            ShowOnMobile = model.ShowOnMobile,

            ShowToGuests = model.ShowToGuests,
            ShowToUsers = model.ShowToUsers,
            ShowToAdmins = model.ShowToAdmins,

            ShowOnDashboard = model.ShowOnDashboard,
            ShowOnPredictionScore = model.ShowOnPredictionScore,
            ShowOnLeaderboard = model.ShowOnLeaderboard,
            ShowOnRules = model.ShowOnRules,
            ShowOnLoginRegister = model.ShowOnLoginRegister,

            ShowAfterPredictionSubmit = model.ShowAfterPredictionSubmit,
            ShowOnlyIfUserHasNoUpcomingPrediction = model.ShowOnlyIfUserHasNoUpcomingPrediction,

            ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim(),
            ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim(),

            CreatedAt = DateTime.Now,
            CreatedByUserId = userId
        };

        _context.Ads.Add(ad);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ad campaign created successfully. Now add slides.";

        return RedirectToAction(nameof(Slides), new { adId = ad.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ad = await _context.Ads
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        return View(ToAdFormViewModel(ad));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ValidateAdDateRange(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ad = await _context.Ads
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        ad.Title = model.Title.Trim();
        ad.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        ad.IsActive = model.IsActive;

        ad.DisplayOrder = model.DisplayOrder;
        ad.Priority = model.Priority;
        ad.SelectionMode = model.SelectionMode;

        ad.StartAt = model.StartAt;
        ad.EndAt = model.EndAt;

        ad.ShowOnDesktop = model.ShowOnDesktop;
        ad.ShowOnTablet = model.ShowOnTablet;
        ad.ShowOnMobile = model.ShowOnMobile;

        ad.ShowToGuests = model.ShowToGuests;
        ad.ShowToUsers = model.ShowToUsers;
        ad.ShowToAdmins = model.ShowToAdmins;

        ad.ShowOnDashboard = model.ShowOnDashboard;
        ad.ShowOnPredictionScore = model.ShowOnPredictionScore;
        ad.ShowOnLeaderboard = model.ShowOnLeaderboard;
        ad.ShowOnRules = model.ShowOnRules;
        ad.ShowOnLoginRegister = model.ShowOnLoginRegister;

        ad.ShowAfterPredictionSubmit = model.ShowAfterPredictionSubmit;
        ad.ShowOnlyIfUserHasNoUpcomingPrediction = model.ShowOnlyIfUserHasNoUpcomingPrediction;

        ad.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim();
        ad.ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim();

        ad.UpdatedAt = DateTime.Now;
        ad.UpdatedByUserId = _userManager.GetUserId(User);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ad campaign updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var ad = await _context.Ads
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        ad.IsActive = !ad.IsActive;
        ad.UpdatedAt = DateTime.Now;
        ad.UpdatedByUserId = _userManager.GetUserId(User);

        await _context.SaveChangesAsync();

        TempData["Success"] = ad.IsActive
            ? "Ad campaign activated."
            : "Ad campaign deactivated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ad = await _context.Ads
            .Include(x => x.Slides)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        ad.IsDeleted = true;
        ad.IsActive = false;
        ad.UpdatedAt = DateTime.Now;
        ad.UpdatedByUserId = _userManager.GetUserId(User);

        foreach (var slide in ad.Slides)
        {
            slide.IsDeleted = true;
            slide.IsActive = false;
            slide.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ad campaign deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Slides(int adId)
    {
        var ad = await _context.Ads
            .Include(x => x.Slides)
            .FirstOrDefaultAsync(x => x.Id == adId && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        var model = new AdSlideIndexViewModel
        {
            AdId = ad.Id,
            AdTitle = ad.Title,
            AdIsActive = ad.IsActive,
            Slides = ad.Slides
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => new AdSlideListItemViewModel
                {
                    Id = x.Id,
                    AdId = x.AdId,
                    Title = x.Title,
                    Description = x.Description,
                    ImagePath = x.ImagePath,
                    AudioPath = x.AudioPath,
                    ButtonText = x.ButtonText,
                    ButtonUrl = x.ButtonUrl,
                    DurationSeconds = x.DurationSeconds,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSlide(int adId)
    {
        var ad = await _context.Ads
            .FirstOrDefaultAsync(x => x.Id == adId && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        var model = new AdSlideFormViewModel
        {
            AdId = ad.Id,
            AdTitle = ad.Title,
            DurationSeconds = 4,
            DisplayOrder = 0,
            IsActive = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlide(AdSlideFormViewModel model)
    {
        var ad = await _context.Ads
            .FirstOrDefaultAsync(x => x.Id == model.AdId && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        model.AdTitle = ad.Title;

        if (model.ImageFile == null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Slide image is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string imagePath;

        try
        {
            imagePath = await SaveAdFileAsync(
                model.ImageFile!,
                "images",
                AllowedImageExtensions,
                MaxImageSizeBytes);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return View(model);
        }

        string? audioPath = null;

        if (model.AudioFile != null)
        {
            try
            {
                audioPath = await SaveAdFileAsync(
                    model.AudioFile,
                    "audio",
                    AllowedAudioExtensions,
                    MaxAudioSizeBytes);
            }
            catch (InvalidOperationException ex)
            {
                DeleteFileIfExists(imagePath);
                ModelState.AddModelError(nameof(model.AudioFile), ex.Message);
                return View(model);
            }
        }

        var slide = new AdSlide
        {
            AdId = ad.Id,
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ImagePath = imagePath,
            AudioPath = audioPath,
            ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim(),
            ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim(),
            DurationSeconds = model.DurationSeconds,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };

        _context.AdSlides.Add(slide);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ad slide created successfully.";

        return RedirectToAction(nameof(Slides), new { adId = ad.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditSlide(int id)
    {
        var slide = await _context.AdSlides
            .Include(x => x.Ad)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.Ad.IsDeleted);

        if (slide == null)
        {
            return NotFound();
        }

        var model = new AdSlideFormViewModel
        {
            Id = slide.Id,
            AdId = slide.AdId,
            AdTitle = slide.Ad.Title,
            Title = slide.Title,
            Description = slide.Description,
            ExistingImagePath = slide.ImagePath,
            ExistingAudioPath = slide.AudioPath,
            ButtonText = slide.ButtonText,
            ButtonUrl = slide.ButtonUrl,
            DurationSeconds = slide.DurationSeconds,
            DisplayOrder = slide.DisplayOrder,
            IsActive = slide.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSlide(int id, AdSlideFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var slide = await _context.AdSlides
            .Include(x => x.Ad)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.Ad.IsDeleted);

        if (slide == null)
        {
            return NotFound();
        }

        model.AdId = slide.AdId;
        model.AdTitle = slide.Ad.Title;
        model.ExistingImagePath = slide.ImagePath;
        model.ExistingAudioPath = slide.AudioPath;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var oldImagePath = slide.ImagePath;
        var oldAudioPath = slide.AudioPath;

        if (model.ImageFile != null)
        {
            try
            {
                slide.ImagePath = await SaveAdFileAsync(
                    model.ImageFile,
                    "images",
                    AllowedImageExtensions,
                    MaxImageSizeBytes);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        if (model.RemoveAudio)
        {
            slide.AudioPath = null;
        }

        if (model.AudioFile != null)
        {
            try
            {
                slide.AudioPath = await SaveAdFileAsync(
                    model.AudioFile,
                    "audio",
                    AllowedAudioExtensions,
                    MaxAudioSizeBytes);
            }
            catch (InvalidOperationException ex)
            {
                if (slide.ImagePath != oldImagePath)
                {
                    DeleteFileIfExists(slide.ImagePath);
                    slide.ImagePath = oldImagePath;
                }

                ModelState.AddModelError(nameof(model.AudioFile), ex.Message);
                return View(model);
            }
        }

        slide.Title = model.Title.Trim();
        slide.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        slide.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? null : model.ButtonText.Trim();
        slide.ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl) ? null : model.ButtonUrl.Trim();
        slide.DurationSeconds = model.DurationSeconds;
        slide.DisplayOrder = model.DisplayOrder;
        slide.IsActive = model.IsActive;
        slide.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        if (model.ImageFile != null && slide.ImagePath != oldImagePath)
        {
            DeleteFileIfExists(oldImagePath);
        }

        if ((model.RemoveAudio || model.AudioFile != null) &&
            !string.IsNullOrWhiteSpace(oldAudioPath) &&
            oldAudioPath != slide.AudioPath)
        {
            DeleteFileIfExists(oldAudioPath);
        }

        TempData["Success"] = "Ad slide updated successfully.";

        return RedirectToAction(nameof(Slides), new { adId = slide.AdId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSlideActive(int id)
    {
        var slide = await _context.AdSlides
            .Include(x => x.Ad)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.Ad.IsDeleted);

        if (slide == null)
        {
            return NotFound();
        }

        slide.IsActive = !slide.IsActive;
        slide.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = slide.IsActive
            ? "Ad slide activated."
            : "Ad slide deactivated.";

        return RedirectToAction(nameof(Slides), new { adId = slide.AdId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlide(int id)
    {
        var slide = await _context.AdSlides
            .Include(x => x.Ad)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.Ad.IsDeleted);

        if (slide == null)
        {
            return NotFound();
        }

        slide.IsDeleted = true;
        slide.IsActive = false;
        slide.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ad slide deleted successfully.";

        return RedirectToAction(nameof(Slides), new { adId = slide.AdId });
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.Today.AddDays(-6);
        var to = toDate?.Date ?? DateTime.Today;

        if (to < from)
        {
            to = from;
        }

        var rangeStart = from.Date;
        var rangeEndExclusive = to.Date.AddDays(1);

        var ads = await _context.Ads
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Title,
                SlideCount = x.Slides.Count(s => !s.IsDeleted)
            })
            .ToListAsync();

        var adIds = ads.Select(x => x.Id).ToList();

        var logsQuery = _context.AdLogs
            .Where(x =>
                x.CreatedAt >= rangeStart &&
                x.CreatedAt < rangeEndExclusive);

        var metricRows = await logsQuery
            .Where(x => x.AdId.HasValue && adIds.Contains(x.AdId.Value))
            .GroupBy(x => new
            {
                AdId = x.AdId!.Value,
                x.EventType
            })
            .Select(g => new
            {
                g.Key.AdId,
                g.Key.EventType,
                Count = g.Count()
            })
            .ToListAsync();

        var metricMap = metricRows.ToDictionary(
            x => $"{x.AdId}|{x.EventType}",
            x => x.Count);

        var items = ads
            .Select(x =>
            {
                var impressions = GetMetric(metricMap, x.Id, "Impression");
                var skips = GetMetric(metricMap, x.Id, "Skip");
                var clicks = GetMetric(metricMap, x.Id, "Click");
                var completes = GetMetric(metricMap, x.Id, "Complete");
                var soundEnabled = GetMetric(metricMap, x.Id, "SoundEnabled");
                var closed = GetMetric(metricMap, x.Id, "Close");

                return new AdAnalyticsItemViewModel
                {
                    AdId = x.Id,
                    AdTitle = x.Title,
                    ImpressionCount = impressions,
                    SkipCount = skips,
                    ClickCount = clicks,
                    CompleteCount = completes,
                    SoundEnabledCount = soundEnabled,
                    CloseCount = closed,
                    ClickThroughRate = CalculateRate(clicks, impressions),
                    SkipRate = CalculateRate(skips, impressions),
                    CompletionRate = CalculateRate(completes, impressions)
                };
            })
            .OrderByDescending(x => x.ImpressionCount)
            .ThenBy(x => x.AdTitle)
            .ToList();

        var deviceRaw = await logsQuery
            .GroupBy(x => new
            {
                DeviceType = string.IsNullOrWhiteSpace(x.DeviceType) ? "Unknown" : x.DeviceType!,
                x.EventType
            })
            .Select(g => new
            {
                g.Key.DeviceType,
                g.Key.EventType,
                Count = g.Count()
            })
            .ToListAsync();

        var deviceItems = deviceRaw
            .GroupBy(x => x.DeviceType)
            .Select(g =>
            {
                var impressions = g.Where(x => x.EventType == "Impression").Sum(x => x.Count);
                var skips = g.Where(x => x.EventType == "Skip").Sum(x => x.Count);
                var clicks = g.Where(x => x.EventType == "Click").Sum(x => x.Count);
                var completes = g.Where(x => x.EventType == "Complete").Sum(x => x.Count);

                return new AdAnalyticsDeviceViewModel
                {
                    DeviceType = g.Key,
                    ImpressionCount = impressions,
                    SkipCount = skips,
                    ClickCount = clicks,
                    CompleteCount = completes,
                    ClickThroughRate = CalculateRate(clicks, impressions),
                    SkipRate = CalculateRate(skips, impressions),
                    CompletionRate = CalculateRate(completes, impressions)
                };
            })
            .OrderByDescending(x => x.ImpressionCount)
            .ThenBy(x => x.DeviceType)
            .ToList();

        var pageRaw = await logsQuery
            .GroupBy(x => new
            {
                PageName = string.IsNullOrWhiteSpace(x.PageName) ? "Unknown" : x.PageName!,
                x.EventType
            })
            .Select(g => new
            {
                g.Key.PageName,
                g.Key.EventType,
                Count = g.Count()
            })
            .ToListAsync();

        var pageItems = pageRaw
            .GroupBy(x => x.PageName)
            .Select(g =>
            {
                var impressions = g.Where(x => x.EventType == "Impression").Sum(x => x.Count);
                var skips = g.Where(x => x.EventType == "Skip").Sum(x => x.Count);
                var clicks = g.Where(x => x.EventType == "Click").Sum(x => x.Count);
                var completes = g.Where(x => x.EventType == "Complete").Sum(x => x.Count);

                return new AdAnalyticsPageViewModel
                {
                    PageName = g.Key,
                    ImpressionCount = impressions,
                    SkipCount = skips,
                    ClickCount = clicks,
                    CompleteCount = completes,
                    ClickThroughRate = CalculateRate(clicks, impressions),
                    SkipRate = CalculateRate(skips, impressions),
                    CompletionRate = CalculateRate(completes, impressions)
                };
            })
            .OrderByDescending(x => x.ImpressionCount)
            .ThenBy(x => x.PageName)
            .ToList();

        var dailyRaw = await logsQuery
            .GroupBy(x => new
            {
                Date = x.CreatedAt.Date,
                x.EventType
            })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.EventType,
                Count = g.Count()
            })
            .ToListAsync();

        var dailyItems = new List<AdAnalyticsDailyViewModel>();

        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var dayRows = dailyRaw.Where(x => x.Date == date).ToList();

            var impressions = dayRows.Where(x => x.EventType == "Impression").Sum(x => x.Count);
            var skips = dayRows.Where(x => x.EventType == "Skip").Sum(x => x.Count);
            var clicks = dayRows.Where(x => x.EventType == "Click").Sum(x => x.Count);
            var completes = dayRows.Where(x => x.EventType == "Complete").Sum(x => x.Count);

            dailyItems.Add(new AdAnalyticsDailyViewModel
            {
                Date = date,
                ImpressionCount = impressions,
                SkipCount = skips,
                ClickCount = clicks,
                CompleteCount = completes,
                ClickThroughRate = CalculateRate(clicks, impressions),
                SkipRate = CalculateRate(skips, impressions),
                CompletionRate = CalculateRate(completes, impressions)
            });
        }

        var eventItems = await logsQuery
            .GroupBy(x => x.EventType)
            .Select(g => new AdAnalyticsEventViewModel
            {
                EventType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var totalImpressions = items.Sum(x => x.ImpressionCount);
        var totalSkips = items.Sum(x => x.SkipCount);
        var totalClicks = items.Sum(x => x.ClickCount);
        var totalCompletes = items.Sum(x => x.CompleteCount);

        var model = new AdAnalyticsViewModel
        {
            FromDate = from,
            ToDate = to,

            TotalAds = ads.Count,
            TotalSlides = ads.Sum(x => x.SlideCount),

            TotalImpressions = totalImpressions,
            TotalSkips = totalSkips,
            TotalClicks = totalClicks,
            TotalCompletes = totalCompletes,
            TotalSoundEnabled = items.Sum(x => x.SoundEnabledCount),
            TotalClosed = items.Sum(x => x.CloseCount),

            ClickThroughRate = CalculateRate(totalClicks, totalImpressions),
            SkipRate = CalculateRate(totalSkips, totalImpressions),
            CompletionRate = CalculateRate(totalCompletes, totalImpressions),

            Items = items,
            DeviceItems = deviceItems,
            PageItems = pageItems,
            DailyItems = dailyItems,
            EventItems = eventItems
        };

        return View(model);
    }

    private static decimal CalculateRate(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0;
        }

        return Math.Round((decimal)numerator * 100 / denominator, 1);
    }

    private static int GetMetric(
        Dictionary<string, int> metricMap,
        int adId,
        string eventType)
    {
        return metricMap.TryGetValue($"{adId}|{eventType}", out var count)
            ? count
            : 0;
    }

    private static AdFormViewModel ToAdFormViewModel(Ad ad)
    {
        return new AdFormViewModel
        {
            Id = ad.Id,
            Title = ad.Title,
            Description = ad.Description,
            IsActive = ad.IsActive,
            DisplayOrder = ad.DisplayOrder,
            Priority = ad.Priority,
            SelectionMode = ad.SelectionMode,
            StartAt = ad.StartAt,
            EndAt = ad.EndAt,

            ShowOnDesktop = ad.ShowOnDesktop,
            ShowOnTablet = ad.ShowOnTablet,
            ShowOnMobile = ad.ShowOnMobile,

            ShowToGuests = ad.ShowToGuests,
            ShowToUsers = ad.ShowToUsers,
            ShowToAdmins = ad.ShowToAdmins,

            ShowOnDashboard = ad.ShowOnDashboard,
            ShowOnPredictionScore = ad.ShowOnPredictionScore,
            ShowOnLeaderboard = ad.ShowOnLeaderboard,
            ShowOnRules = ad.ShowOnRules,
            ShowOnLoginRegister = ad.ShowOnLoginRegister,

            ShowAfterPredictionSubmit = ad.ShowAfterPredictionSubmit,
            ShowOnlyIfUserHasNoUpcomingPrediction = ad.ShowOnlyIfUserHasNoUpcomingPrediction,

            ButtonText = ad.ButtonText,
            ButtonUrl = ad.ButtonUrl
        };
    }

    private void ValidateAdDateRange(AdFormViewModel model)
    {
        if (model.StartAt.HasValue &&
            model.EndAt.HasValue &&
            model.EndAt.Value <= model.StartAt.Value)
        {
            ModelState.AddModelError(
                nameof(model.EndAt),
                "End date/time must be greater than start date/time.");
        }
    }

    private async Task<string> SaveAdFileAsync(
        IFormFile file,
        string folderName,
        string[] allowedExtensions,
        long maxBytes)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"File size must be less than {maxBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
        }

        var root = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var relativeFolder = Path.Combine("uploads", "ads", folderName);
        var absoluteFolder = Path.Combine(root, relativeFolder);

        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);

        await using var stream = new FileStream(absolutePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Path.Combine(relativeFolder, fileName).Replace("\\", "/");
    }

    private void DeleteFileIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var root = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var normalized = relativePath
            .TrimStart('~')
            .TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(root, normalized);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var ad = await _context.Ads
            .Include(x => x.Slides)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (ad == null)
        {
            return NotFound();
        }

        var slides = ad.Slides
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new AdsRenderSlideViewModel
            {
                SlideId = x.Id,
                Title = x.Title,
                Description = x.Description,
                ImageUrl = Url.Content("~/" + x.ImagePath.TrimStart('/', '~')),
                AudioUrl = string.IsNullOrWhiteSpace(x.AudioPath)
                    ? null
                    : Url.Content("~/" + x.AudioPath.TrimStart('/', '~')),
                ButtonText = string.IsNullOrWhiteSpace(x.ButtonText)
                    ? ad.ButtonText
                    : x.ButtonText,
                ButtonUrl = string.IsNullOrWhiteSpace(x.ButtonUrl)
                    ? ad.ButtonUrl
                    : x.ButtonUrl,
                DurationSeconds = x.DurationSeconds <= 0 ? 4 : x.DurationSeconds
            })
            .ToList();

        if (!slides.Any())
        {
            TempData["Error"] = "This ad has no slides to preview.";
            return RedirectToAction(nameof(Slides), new { adId = ad.Id });
        }

        var model = new AdsRenderViewModel
        {
            ShouldRender = true,
            IsPreview = true,

            AdId = ad.Id,
            AdTitle = ad.Title,

            ShowAfterSeconds = 0,
            MandatoryWatchSeconds = 0,
            AutoCloseSeconds = 0,
            DefaultSlideDurationSeconds = 4,

            ShowSkipButton = true,
            ShowCountdown = true,
            ShowMuteButton = true,
            RequireTapForSound = true,

            ShowOncePerSession = false,
            ShowOncePerDay = false,
            MaxImpressionsPerDayPerUser = 0,

            TrackImpression = false,
            TrackSkip = false,
            TrackClick = false,
            TrackComplete = false,
            TrackSoundEnabled = false,

            PageName = "Preview",
            Slides = slides
        };

        return View(model);
    }
}