using FootballPredictionGame.Models;

namespace FootballPredictionGame.Services;

public class WeeklyPerformanceEmailService : IWeeklyPerformanceEmailService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public WeeklyPerformanceEmailService(
        IEmailService emailService,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task SendAppreciationEmailAsync(
        ApplicationUser user,
        int weeklyPoint,
        int predictionCount,
        int exactPredictionCount,
        int missedPredictionCount,
        DateTime weekStart,
        DateTime weekEnd)
    {
        var motherUrl = _configuration["AppSettings:MotherUrl"] ?? "";
        var scoreUrl = motherUrl.TrimEnd('/') + "/Dashboard/PredictionScore";

        var subject = "Great performance this week - Transtec 360° Football Prediction";

        var emailBody = $@"
<div style='font-family:Arial,sans-serif;padding:20px;background:#f5f8ff;'>
    <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #b9d7ff;'>

        <div style='text-align:center;margin-bottom:20px;'>
            <div style='font-size:48px;'>🏆</div>
            <h2 style='color:#099b49;margin:10px 0 5px;'>Excellent Performance!</h2>
            <p style='color:#64748b;margin:0;'>Weekly performance summary</p>
        </div>

        <p>Dear {user.FullName},</p>

        <p>
            Congratulations! You performed very well in the <strong>Transtec 360° Football Prediction</strong> challenge this week.
        </p>

        <div style='background:#ecfdf5;border:1px solid #86efac;border-radius:12px;padding:18px;margin:22px 0;'>
            <h3 style='margin:0 0 12px;color:#047857;'>Your Weekly Summary</h3>

            <p style='margin:6px 0;color:#334155;'>Week: <strong>{weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Weekly Points: <strong>{weeklyPoint}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Predictions Submitted: <strong>{predictionCount}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Exact Predictions: <strong>{exactPredictionCount}</strong></p>
        </div>

        <p>
            Keep predicting regularly and stay close to the leaderboard top position.
        </p>

        <p style='text-align:center;margin:30px 0;'>
            <a href='{scoreUrl}'
               style='background:#ffc107;border:1px solid #e0a800;border-radius:8px;
                      color:#212529 !important;display:inline-block;font-family:Arial,sans-serif;
                      font-size:18px;font-weight:700;line-height:24px;padding:14px 32px;
                      text-align:center;text-decoration:none;min-width:220px;'>
                View My Score
            </a>
        </p>

        <p style='font-size:13px;color:#64748b;'>
            Thank you for your active participation.
        </p>

    </div>
</div>";

        await _emailService.SendEmailAsync(user.Email!, subject, emailBody);
    }

    public async Task SendImprovementEmailAsync(
        ApplicationUser user,
        int weeklyPoint,
        int predictionCount,
        int exactPredictionCount,
        int missedPredictionCount,
        DateTime weekStart,
        DateTime weekEnd)
    {
        var motherUrl = _configuration["AppSettings:MotherUrl"] ?? "";
        var scoreUrl = motherUrl.TrimEnd('/') + "/Dashboard/PredictionScore";

        var subject = "Improve your prediction score this week";

        var emailBody = $@"
<div style='font-family:Arial,sans-serif;padding:20px;background:#f5f8ff;'>
    <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #b9d7ff;'>

        <div style='text-align:center;margin-bottom:20px;'>
            <div style='font-size:48px;'>⚽</div>
            <h2 style='color:#f97316;margin:10px 0 5px;'>Keep Going!</h2>
            <p style='color:#64748b;margin:0;'>Weekly prediction improvement note</p>
        </div>

        <p>Dear {user.FullName},</p>

        <p>
            You still have many chances to improve your position in the <strong>Transtec 360° Football Prediction</strong> challenge.
        </p>

        <div style='background:#fff7ed;border:1px solid #fdba74;border-radius:12px;padding:18px;margin:22px 0;'>
            <h3 style='margin:0 0 12px;color:#c2410c;'>Your Weekly Summary</h3>

            <p style='margin:6px 0;color:#334155;'>Week: <strong>{weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Weekly Points: <strong>{weeklyPoint}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Predictions Submitted: <strong>{predictionCount}</strong></p>
            <p style='margin:6px 0;color:#334155;'>Missed Predictions: <strong>{missedPredictionCount}</strong></p>
        </div>

        <p>
            Try to submit predictions before every match. Regular participation can quickly improve your total score.
        </p>

        <p style='text-align:center;margin:30px 0;'>
            <a href='{scoreUrl}'
               style='background:#ffc107;border:1px solid #e0a800;border-radius:8px;
                      color:#212529 !important;display:inline-block;font-family:Arial,sans-serif;
                      font-size:18px;font-weight:700;line-height:24px;padding:14px 32px;
                      text-align:center;text-decoration:none;min-width:220px;'>
                Submit Upcoming Predictions
            </a>
        </p>

        <p style='font-size:13px;color:#64748b;'>
            Stay active, predict regularly, and enjoy the challenge.
        </p>

    </div>
</div>";

        await _emailService.SendEmailAsync(user.Email!, subject, emailBody);
    }
}