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
}