namespace FootballPredictionGame.ViewModels;

public class AdsRenderViewModel
{
    public bool ShouldRender { get; set; }
    public bool IsPreview { get; set; }

    public int AdId { get; set; }

    public string AdTitle { get; set; } = string.Empty;
    public string TriggerMode { get; set; } = "Timer";

    public bool ShowAfterPredictionSubmit { get; set; }

    public bool ShowOnlyIfUserHasNoUpcomingPrediction { get; set; }


    public int ShowAfterSeconds { get; set; }

    public int MandatoryWatchSeconds { get; set; }

    public int AutoCloseSeconds { get; set; }

    public int DefaultSlideDurationSeconds { get; set; }

    public bool ShowSkipButton { get; set; }

    public bool ShowCountdown { get; set; }

    public bool ShowMuteButton { get; set; }

    public bool RequireTapForSound { get; set; }

    public bool ShowOncePerSession { get; set; }

    public bool ShowOncePerDay { get; set; }

    public int MaxImpressionsPerDayPerUser { get; set; }

    public bool TrackImpression { get; set; }

    public bool TrackSkip { get; set; }

    public bool TrackClick { get; set; }

    public bool TrackComplete { get; set; }

    public bool TrackSoundEnabled { get; set; }

    public string PageName { get; set; } = string.Empty;

    public List<AdsRenderSlideViewModel> Slides { get; set; } = new();
}

public class AdsRenderSlideViewModel
{
    public int SlideId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? AudioUrl { get; set; }

    public string? ButtonText { get; set; }

    public string? ButtonUrl { get; set; }

    public int DurationSeconds { get; set; }
}

public class AdTrackRequestViewModel
{
    public int? AdId { get; set; }

    public int? AdSlideId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? DeviceType { get; set; }

    public string? PageName { get; set; }

    public string? PageUrl { get; set; }

    public string? ExtraDataJson { get; set; }
}