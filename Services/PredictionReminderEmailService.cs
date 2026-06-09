using FootballPredictionGame.Models;
using Microsoft.AspNetCore.Routing;

namespace FootballPredictionGame.Services;

public class PredictionReminderEmailService : IPredictionReminderEmailService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public PredictionReminderEmailService(
        IEmailService emailService,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task SendPredictionReminderAsync(
        ApplicationUser user,
        Fixture fixture,
        string reminderType)
    {
        var motherUrl = _configuration["AppSettings:MotherUrl"] ?? "";
        var predictionUrl = motherUrl.TrimEnd('/') + "/Dashboard";

        var reminderTitle = reminderType == "1Hour"
            ? "Final Call: Submit Your Prediction"
            : "Prediction Reminder";

        var urgencyText = reminderType == "1Hour"
            ? "This is your final reminder. Prediction will close when the match starts."
            : "The match will start soon. Please submit your prediction before kickoff.";

        var subject = reminderType == "1Hour"
            ? $"Final reminder: {fixture.TeamOneName} vs {fixture.TeamTwoName}"
            : $"Prediction reminder: {fixture.TeamOneName} vs {fixture.TeamTwoName}";

        var emailBody = $@"
<div style='font-family:Arial,sans-serif;padding:20px;background:#f5f8ff;'>
    <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #b9d7ff;'>

        <div style='text-align:center;margin-bottom:20px;'>
            <div style='font-size:48px;'>⚽</div>
            <h2 style='color:#155dfc;margin:10px 0 5px;'>{reminderTitle}</h2>
            <p style='color:#64748b;margin:0;'>Transtec 360° Football Prediction</p>
        </div>

        <p>Dear {user.FullName},</p>

        <p>
            You have not submitted your prediction yet for the following match.
        </p>

        <div style='background:#eef6ff;border:1px solid #b9d7ff;border-radius:12px;padding:18px;margin:22px 0;text-align:center;'>
            <h3 style='margin:0 0 10px;color:#0f172a;'>{fixture.TeamOneName} vs {fixture.TeamTwoName}</h3>

            <p style='margin:0;color:#334155;font-size:15px;'>
                Match Time: <strong>{fixture.MatchDateTime:dd MMM yyyy, hh:mm tt}</strong>
            </p>
        </div>

        <p style='background:#fff7ed;border:1px solid #fed7aa;border-radius:10px;padding:14px;color:#7c2d12;'>
            {urgencyText}
        </p>

        <p style='text-align:center;margin:30px 0;'>
            <a href='{predictionUrl}'
               style='background:#ffc107;border:1px solid #e0a800;border-radius:8px;
                      color:#212529 !important;display:inline-block;font-family:Arial,sans-serif;
                      font-size:18px;font-weight:700;line-height:24px;padding:14px 32px;
                      text-align:center;text-decoration:none;min-width:220px;'>
                Submit Prediction
            </a>
        </p>

        <p style='font-size:13px;color:#64748b;'>
            Please ignore this email if you have already submitted your prediction recently.
        </p>

    </div>
</div>";

        await _emailService.SendEmailAsync(
            user.Email!,
            subject,
            emailBody
        );
    }
}