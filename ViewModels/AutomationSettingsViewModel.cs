// ViewModels/AutomationSettingsViewModel.cs
using FootballPredictionGame.Models;
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.ViewModels;

public class AutomationSettingsViewModel
{
    public int Id { get; set; }

    [Display(Name = "Master Automation Enabled")]
    public bool MasterAutomationEnabled { get; set; }

    [Display(Name = "Fixture Automation Enabled")]
    public bool FixtureAutomationEnabled { get; set; }

    [Display(Name = "Auto Update Upcoming to Live")]
    public bool AutoUpcomingToLiveEnabled { get; set; }

    [Display(Name = "Auto Update Live to Finished")]
    public bool AutoLiveToFinishedEnabled { get; set; }

    [Display(Name = "Auto Result Processing")]
    public bool AutoResultProcessingEnabled { get; set; }

    [Display(Name = "Reminder Automation Enabled")]
    public bool ReminderAutomationEnabled { get; set; }

    [Display(Name = "24 Hour Reminder Enabled")]
    public bool Reminder24HourEnabled { get; set; }

    [Display(Name = "1 Hour Reminder Enabled")]
    public bool Reminder1HourEnabled { get; set; }

    [Display(Name = "Weekly Email Automation Enabled")]
    public bool WeeklyEmailAutomationEnabled { get; set; }

    [Display(Name = "Weekly Appreciation Email Enabled")]
    public bool WeeklyAppreciationEmailEnabled { get; set; }

    [Display(Name = "Weekly Improvement Email Enabled")]
    public bool WeeklyImprovementEmailEnabled { get; set; }

    [Range(0, 100)]
    [Display(Name = "Good Performance Point Threshold")]
    public int WeeklyGoodPerformancePointThreshold { get; set; }

    [Range(0, 100)]
    [Display(Name = "Poor Performance Point Threshold")]
    public int WeeklyPoorPerformancePointThreshold { get; set; }

    [Range(0, 100)]
    [Display(Name = "Missed Prediction Threshold")]
    public int WeeklyMissedPredictionThreshold { get; set; }

    [Display(Name = "AI User Approval Enabled")]
    public bool AiUserApprovalEnabled { get; set; }

    [Display(Name = "AI Auto Approve Enabled")]
    public bool AiAutoApproveEnabled { get; set; }

    [Range(0, 100)]
    [Display(Name = "AI Auto Approve Risk Threshold")]
    public int AiAutoApproveRiskThreshold { get; set; }

    [Display(Name = "Login Tracking Enabled")]
    public bool LoginTrackingEnabled { get; set; }

    [Display(Name = "Active Session Tracking Enabled")]
    public bool ActiveSessionTrackingEnabled { get; set; }

    [Range(1, 120)]
    [Display(Name = "Session Timeout Minutes")]
    public int SessionTimeoutMinutes { get; set; }

    [Display(Name = "Auto Activate User After Email Verification")]
    public bool AutoActivateUserAfterEmailVerification { get; set; }

    [Display(Name = "Show WhatsApp Join Menu")]
    public bool ShowWhatsAppJoinMenu { get; set; }

    [Display(Name = "WhatsApp Group URL")]
    [MaxLength(500)]
    public string? WhatsAppGroupUrl { get; set; }

    [Display(Name = "Execution Mode")]
    public AutomationExecutionMode ExecutionMode { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Fixture Check Interval Minutes")]
    public int FixtureCheckIntervalMinutes { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Reminder Check Interval Minutes")]
    public int ReminderCheckIntervalMinutes { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Score Sync Interval Minutes")]
    public int ScoreSyncIntervalMinutes { get; set; }

    [Range(1, 1440)]
    [Display(Name = "AI User Review Interval Minutes")]
    public int AiUserReviewIntervalMinutes { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Session Cleanup Interval Minutes")]
    public int SessionCleanupIntervalMinutes { get; set; }

    [Display(Name = "Weekly Email Send Day")]
    public DayOfWeek WeeklyEmailSendDay { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Weekly Email Send Time")]
    public TimeSpan WeeklyEmailSendTime { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Range(30, 300)]
    [Display(Name = "Default Match Duration Minutes")]
    public int DefaultMatchDurationMinutes { get; set; }

    [Display(Name = "Ads Enabled")]
    public bool AdsEnabled { get; set; }

    [Display(Name = "Show Ads on Desktop")]
    public bool AdsShowOnDesktop { get; set; }

    [Display(Name = "Show Ads on Tablet")]
    public bool AdsShowOnTablet { get; set; }

    [Display(Name = "Show Ads on Mobile")]
    public bool AdsShowOnMobile { get; set; }

    [Display(Name = "Show Ads to Guests")]
    public bool AdsShowToGuests { get; set; }

    [Display(Name = "Show Ads to Users")]
    public bool AdsShowToUsers { get; set; }

    [Display(Name = "Show Ads to Admins")]
    public bool AdsShowToAdmins { get; set; }

    [Display(Name = "Show Ads on Dashboard")]
    public bool AdsShowOnDashboard { get; set; }

    [Display(Name = "Show Ads on Prediction Score")]
    public bool AdsShowOnPredictionScore { get; set; }

    [Display(Name = "Show Ads on Leaderboard")]
    public bool AdsShowOnLeaderboard { get; set; }

    [Display(Name = "Show Ads on Rules Page")]
    public bool AdsShowOnRules { get; set; }

    [Display(Name = "Show Ads on Login/Register")]
    public bool AdsShowOnLoginRegister { get; set; }

    [Range(0, 300)]
    [Display(Name = "Show After Seconds")]
    public int AdsShowAfterSeconds { get; set; }

    [Range(0, 120)]
    [Display(Name = "Mandatory Watch Seconds")]
    public int AdsMandatoryWatchSeconds { get; set; }

    [Range(5, 300)]
    [Display(Name = "Auto Close Seconds")]
    public int AdsAutoCloseSeconds { get; set; }

    [Range(1, 60)]
    [Display(Name = "Default Slide Duration Seconds")]
    public int AdsDefaultSlideDurationSeconds { get; set; }

    [Display(Name = "Show Skip Button")]
    public bool AdsShowSkipButton { get; set; }

    [Display(Name = "Show Countdown")]
    public bool AdsShowCountdown { get; set; }

    [Display(Name = "Show Mute Button")]
    public bool AdsShowMuteButton { get; set; }

    [Display(Name = "Require Tap for Sound")]
    public bool AdsRequireTapForSound { get; set; }

    [Display(Name = "Show Once Per Session")]
    public bool AdsShowOncePerSession { get; set; }

    [Display(Name = "Show Once Per Day")]
    public bool AdsShowOncePerDay { get; set; }

    [Range(1, 100)]
    [Display(Name = "Max Impressions Per Day Per User")]
    public int AdsMaxImpressionsPerDayPerUser { get; set; }

    [Display(Name = "Track Impression")]
    public bool AdsTrackImpression { get; set; }

    [Display(Name = "Track Skip")]
    public bool AdsTrackSkip { get; set; }

    [Display(Name = "Track Click")]
    public bool AdsTrackClick { get; set; }

    [Display(Name = "Track Complete")]
    public bool AdsTrackComplete { get; set; }

    [Display(Name = "Track Sound Enabled")]
    public bool AdsTrackSoundEnabled { get; set; }

    [Display(Name = "Enable Ads Schedule")]
    public bool AdsEnableSchedule { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Ads Start At")]
    public DateTime? AdsStartAt { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Ads End At")]
    public DateTime? AdsEndAt { get; set; }

}