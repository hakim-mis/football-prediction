using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.ViewComponents;

public class AdsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdsViewComponent(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(string triggerMode = "Timer")
    {
        var emptyModel = new AdsRenderViewModel
        {
            ShouldRender = false
        };

        var settings = await _context.AutomationSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings == null || !settings.AdsEnabled)
        {
            return View(emptyModel);
        }

        if (settings.AdsEnableSchedule)
        {
            var now = DateTime.Now;

            if (settings.AdsStartAt.HasValue && now < settings.AdsStartAt.Value)
            {
                return View(emptyModel);
            }

            if (settings.AdsEndAt.HasValue && now > settings.AdsEndAt.Value)
            {
                return View(emptyModel);
            }
        }

        var pageName = GetPageName();

        if (!IsPageAllowed(settings, pageName))
        {
            return View(emptyModel);
        }

        var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated == true;
        var isAdmin = HttpContext.User.IsInRole("Admin");
        var userId = _userManager.GetUserId(HttpContext.User);

        if (!IsAudienceAllowed(settings, isAuthenticated, isAdmin))
        {
            return View(emptyModel);
        }

        var nowTime = DateTime.Now;

        var adsQuery = _context.Ads
            .Include(x => x.Slides)
            .Where(x =>
                x.IsActive &&
                !x.IsDeleted &&
                (!x.StartAt.HasValue || x.StartAt.Value <= nowTime) &&
                (!x.EndAt.HasValue || x.EndAt.Value >= nowTime));

        adsQuery = ApplyPageFilter(adsQuery, pageName);
        adsQuery = ApplyAudienceFilter(adsQuery, isAuthenticated, isAdmin);
        adsQuery = ApplyTriggerFilter(adsQuery, triggerMode);

        var ads = await adsQuery
            .Where(x => x.Slides.Any(s => s.IsActive && !s.IsDeleted))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        if (!ads.Any())
        {
            return View(emptyModel);
        }

        if (isAuthenticated && !string.IsNullOrWhiteSpace(userId))
        {
            ads = await ApplySmartUserRulesAsync(ads, userId);
        }
        else
        {
            ads = ads
                .Where(x => !x.ShowOnlyIfUserHasNoUpcomingPrediction)
                .ToList();
        }

        if (!ads.Any())
        {
            return View(emptyModel);
        }

        var selectedAd = SelectAd(ads);

        if (selectedAd == null)
        {
            return View(emptyModel);
        }

        var slides = selectedAd.Slides
            .Where(x => x.IsActive && !x.IsDeleted)
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
                    ? selectedAd.ButtonText
                    : x.ButtonText,
                ButtonUrl = string.IsNullOrWhiteSpace(x.ButtonUrl)
                    ? selectedAd.ButtonUrl
                    : x.ButtonUrl,
                DurationSeconds = x.DurationSeconds <= 0
                    ? settings.AdsDefaultSlideDurationSeconds
                    : x.DurationSeconds
            })
            .ToList();

        if (!slides.Any())
        {
            return View(emptyModel);
        }

        var model = new AdsRenderViewModel
        {
            ShouldRender = true,
            IsPreview = false,

            AdId = selectedAd.Id,
            AdTitle = selectedAd.Title,

            TriggerMode = triggerMode,
            ShowAfterPredictionSubmit = selectedAd.ShowAfterPredictionSubmit,
            ShowOnlyIfUserHasNoUpcomingPrediction = selectedAd.ShowOnlyIfUserHasNoUpcomingPrediction,

            ShowAfterSeconds = triggerMode.Equals("AfterPredictionSubmit", StringComparison.OrdinalIgnoreCase)
                ? 0
                : settings.AdsShowAfterSeconds,

            MandatoryWatchSeconds = settings.AdsMandatoryWatchSeconds,
            AutoCloseSeconds = settings.AdsAutoCloseSeconds,
            DefaultSlideDurationSeconds = settings.AdsDefaultSlideDurationSeconds,

            ShowSkipButton = settings.AdsShowSkipButton,
            ShowCountdown = settings.AdsShowCountdown,
            ShowMuteButton = settings.AdsShowMuteButton,
            RequireTapForSound = settings.AdsRequireTapForSound,

            ShowOncePerSession = settings.AdsShowOncePerSession,
            ShowOncePerDay = settings.AdsShowOncePerDay,
            MaxImpressionsPerDayPerUser = settings.AdsMaxImpressionsPerDayPerUser,

            TrackImpression = settings.AdsTrackImpression,
            TrackSkip = settings.AdsTrackSkip,
            TrackClick = settings.AdsTrackClick,
            TrackComplete = settings.AdsTrackComplete,
            TrackSoundEnabled = settings.AdsTrackSoundEnabled,

            PageName = pageName,
            Slides = slides
        };

        return View(model);
    }

    private async Task<List<Ad>> ApplySmartUserRulesAsync(List<Ad> ads, string userId)
    {
        var needsUpcomingPredictionCheck = ads.Any(x => x.ShowOnlyIfUserHasNoUpcomingPrediction);

        if (!needsUpcomingPredictionCheck)
        {
            return ads;
        }

        var now = DateTime.Now;

        var upcomingFixtureIds = await _context.Fixtures
            .Where(x =>
                (x.Status == MatchStatus.Upcoming || x.Status == MatchStatus.Live) &&
                x.MatchDateTime >= now.AddHours(-4))
            .Select(x => x.Id)
            .ToListAsync();

        if (!upcomingFixtureIds.Any())
        {
            return ads
                .Where(x => !x.ShowOnlyIfUserHasNoUpcomingPrediction)
                .ToList();
        }

        var userPredictedFixtureIds = await _context.Predictions
            .Where(x =>
                x.UserId == userId &&
                upcomingFixtureIds.Contains(x.FixtureId))
            .Select(x => x.FixtureId)
            .Distinct()
            .ToListAsync();

        var hasMissingUpcomingPrediction = upcomingFixtureIds
            .Any(id => !userPredictedFixtureIds.Contains(id));

        if (hasMissingUpcomingPrediction)
        {
            return ads;
        }

        return ads
            .Where(x => !x.ShowOnlyIfUserHasNoUpcomingPrediction)
            .ToList();
    }

    private string GetPageName()
    {
        var area = RouteData.Values["area"]?.ToString() ?? "";
        var controller = RouteData.Values["controller"]?.ToString() ?? "";
        var action = RouteData.Values["action"]?.ToString() ?? "";

        if (area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Admin";
        }

        if (controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return "Dashboard";
        }

        if (controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("PredictionScore", StringComparison.OrdinalIgnoreCase))
        {
            return "PredictionScore";
        }

        if (controller.Equals("Leaderboard", StringComparison.OrdinalIgnoreCase))
        {
            return "Leaderboard";
        }

        if (controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("Rules", StringComparison.OrdinalIgnoreCase))
        {
            return "Rules";
        }

        if (controller.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
            controller.Equals("Auth", StringComparison.OrdinalIgnoreCase))
        {
            return "LoginRegister";
        }

        return $"{controller}.{action}";
    }

    private static bool IsPageAllowed(AutomationSettings settings, string pageName)
    {
        return pageName switch
        {
            "Dashboard" => settings.AdsShowOnDashboard,
            "PredictionScore" => settings.AdsShowOnPredictionScore,
            "Leaderboard" => settings.AdsShowOnLeaderboard,
            "Rules" => settings.AdsShowOnRules,
            "LoginRegister" => settings.AdsShowOnLoginRegister,
            "Admin" => settings.AdsShowToAdmins,
            _ => true
        };
    }

    private static bool IsAudienceAllowed(
        AutomationSettings settings,
        bool isAuthenticated,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return settings.AdsShowToAdmins;
        }

        if (isAuthenticated)
        {
            return settings.AdsShowToUsers;
        }

        return settings.AdsShowToGuests;
    }

    private static IQueryable<Ad> ApplyPageFilter(
        IQueryable<Ad> query,
        string pageName)
    {
        return pageName switch
        {
            "Dashboard" => query.Where(x => x.ShowOnDashboard),
            "PredictionScore" => query.Where(x => x.ShowOnPredictionScore),
            "Leaderboard" => query.Where(x => x.ShowOnLeaderboard),
            "Rules" => query.Where(x => x.ShowOnRules),
            "LoginRegister" => query.Where(x => x.ShowOnLoginRegister),
            "Admin" => query.Where(x => x.ShowToAdmins),
            _ => query
        };
    }

    private static IQueryable<Ad> ApplyAudienceFilter(
        IQueryable<Ad> query,
        bool isAuthenticated,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return query.Where(x => x.ShowToAdmins);
        }

        if (isAuthenticated)
        {
            return query.Where(x => x.ShowToUsers);
        }

        return query.Where(x => x.ShowToGuests);
    }

    private static IQueryable<Ad> ApplyTriggerFilter(
        IQueryable<Ad> query,
        string triggerMode)
    {
        if (triggerMode.Equals("AfterPredictionSubmit", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(x => x.ShowAfterPredictionSubmit);
        }

        return query.Where(x => !x.ShowAfterPredictionSubmit);
    }

    private static Ad? SelectAd(List<Ad> ads)
    {
        if (!ads.Any())
        {
            return null;
        }

        var priorityAds = ads
            .Where(x => x.SelectionMode == AdSelectionMode.Priority)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DisplayOrder)
            .ToList();

        if (priorityAds.Any())
        {
            return priorityAds.First();
        }

        var randomAds = ads
            .Where(x => x.SelectionMode == AdSelectionMode.Random)
            .ToList();

        if (randomAds.Any())
        {
            var random = new Random();
            return randomAds[random.Next(randomAds.Count)];
        }

        return ads
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Priority)
            .FirstOrDefault();
    }
}