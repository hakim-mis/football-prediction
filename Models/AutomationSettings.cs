// Models/AutomationSettings.cs
using System.ComponentModel.DataAnnotations;

namespace FootballPredictionGame.Models;

public class AutomationSettings
{
    public int Id { get; set; }

    // Main switch
    public bool MasterAutomationEnabled { get; set; } = false;

    // Fixture automation
    public bool FixtureAutomationEnabled { get; set; } = false;
    public bool AutoUpcomingToLiveEnabled { get; set; } = false;
    public bool AutoLiveToFinishedEnabled { get; set; } = false;
    public bool AutoResultProcessingEnabled { get; set; } = false;

    // Prediction reminder automation
    public bool ReminderAutomationEnabled { get; set; } = false;
    public bool Reminder24HourEnabled { get; set; } = false;
    public bool Reminder1HourEnabled { get; set; } = false;

    // Weekly performance email
    public bool WeeklyEmailAutomationEnabled { get; set; } = false;
    public bool WeeklyAppreciationEmailEnabled { get; set; } = false;
    public bool WeeklyImprovementEmailEnabled { get; set; } = false;

    [Range(0, 100)]
    public int WeeklyGoodPerformancePointThreshold { get; set; } = 10;

    [Range(0, 100)]
    public int WeeklyPoorPerformancePointThreshold { get; set; } = 2;

    [Range(0, 100)]
    public int WeeklyMissedPredictionThreshold { get; set; } = 5;

    // AI user approval
    public bool AiUserApprovalEnabled { get; set; } = false;
    public bool AiAutoApproveEnabled { get; set; } = false;

    [Range(0, 100)]
    public int AiAutoApproveRiskThreshold { get; set; } = 25;

    // Login tracking
    public bool LoginTrackingEnabled { get; set; } = true;
    public bool ActiveSessionTrackingEnabled { get; set; } = true;

    [Range(1, 120)]
    public int SessionTimeoutMinutes { get; set; } = 10;

    // Portal / Registration settings
    public bool AutoActivateUserAfterEmailVerification { get; set; } = false;

    public bool ShowWhatsAppJoinMenu { get; set; } = true;

    [MaxLength(500)]
    public string? WhatsAppGroupUrl { get; set; }

    // Execution mode
    public AutomationExecutionMode ExecutionMode { get; set; } = AutomationExecutionMode.SuggestOnly;

    // Job interval settings
    [Range(1, 1440)]
    public int FixtureCheckIntervalMinutes { get; set; } = 5;

    [Range(1, 1440)]
    public int ReminderCheckIntervalMinutes { get; set; } = 15;

    [Range(1, 1440)]
    public int ScoreSyncIntervalMinutes { get; set; } = 5;

    [Range(1, 1440)]
    public int AiUserReviewIntervalMinutes { get; set; } = 30;

    [Range(1, 1440)]
    public int SessionCleanupIntervalMinutes { get; set; } = 5;

    // Weekly schedule
    public DayOfWeek WeeklyEmailSendDay { get; set; } = DayOfWeek.Monday;

    [DataType(DataType.Time)]
    public TimeSpan WeeklyEmailSendTime { get; set; } = new TimeSpan(9, 0, 0);

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }

    [Range(30, 300)]
    public int DefaultMatchDurationMinutes { get; set; } = 120;

    // Ads global settings
    public bool AdsEnabled { get; set; } = false;

    // Ads platform control
    public bool AdsShowOnDesktop { get; set; } = true;
    public bool AdsShowOnTablet { get; set; } = true;
    public bool AdsShowOnMobile { get; set; } = true;

    // Ads audience control
    public bool AdsShowToGuests { get; set; } = true;
    public bool AdsShowToUsers { get; set; } = true;
    public bool AdsShowToAdmins { get; set; } = false;

    // Ads page control
    public bool AdsShowOnDashboard { get; set; } = true;
    public bool AdsShowOnPredictionScore { get; set; } = true;
    public bool AdsShowOnLeaderboard { get; set; } = true;
    public bool AdsShowOnRules { get; set; } = false;
    public bool AdsShowOnLoginRegister { get; set; } = false;

    // Ads timing
    [Range(0, 300)]
    public int AdsShowAfterSeconds { get; set; } = 25;

    [Range(0, 120)]
    public int AdsMandatoryWatchSeconds { get; set; } = 5;

    [Range(5, 300)]
    public int AdsAutoCloseSeconds { get; set; } = 20;

    [Range(1, 60)]
    public int AdsDefaultSlideDurationSeconds { get; set; } = 4;

    // Ads skip and audio
    public bool AdsShowSkipButton { get; set; } = true;
    public bool AdsShowCountdown { get; set; } = true;
    public bool AdsShowMuteButton { get; set; } = true;
    public bool AdsRequireTapForSound { get; set; } = true;

    // Ads frequency
    public bool AdsShowOncePerSession { get; set; } = true;
    public bool AdsShowOncePerDay { get; set; } = false;

    [Range(1, 100)]
    public int AdsMaxImpressionsPerDayPerUser { get; set; } = 3;

    // Ads tracking
    public bool AdsTrackImpression { get; set; } = true;
    public bool AdsTrackSkip { get; set; } = true;
    public bool AdsTrackClick { get; set; } = true;
    public bool AdsTrackComplete { get; set; } = true;
    public bool AdsTrackSoundEnabled { get; set; } = true;

    // Ads schedule
    public bool AdsEnableSchedule { get; set; } = false;
    public DateTime? AdsStartAt { get; set; }
    public DateTime? AdsEndAt { get; set; }
}