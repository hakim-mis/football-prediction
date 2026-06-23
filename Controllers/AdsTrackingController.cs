using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FootballPredictionGame.Controllers;

[Route("ads-tracking")]
public class AdsTrackingController : Controller
{
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Impression",
        "Skip",
        "Complete",
        "Click",
        "SoundEnabled",
        "Close"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdsTrackingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost("track")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Track([FromBody] AdTrackRequestViewModel model)
    {
        if (model == null ||
            string.IsNullOrWhiteSpace(model.EventType) ||
            !AllowedEvents.Contains(model.EventType))
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid tracking event."
            });
        }

        var sessionId = HttpContext.Session.Id;
        var userId = _userManager.GetUserId(User);

        var log = new AdLog
        {
            AdId = model.AdId,
            AdSlideId = model.AdSlideId,
            UserId = userId,
            SessionId = sessionId,
            EventType = model.EventType.Trim(),
            DeviceType = string.IsNullOrWhiteSpace(model.DeviceType) ? null : model.DeviceType.Trim(),
            PageName = string.IsNullOrWhiteSpace(model.PageName) ? null : model.PageName.Trim(),
            PageUrl = string.IsNullOrWhiteSpace(model.PageUrl) ? null : model.PageUrl.Trim(),
            IpAddress = GetClientIp(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            ExtraDataJson = string.IsNullOrWhiteSpace(model.ExtraDataJson) ? null : model.ExtraDataJson,
            CreatedAt = DateTime.Now
        };

        _context.AdLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true
        });
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}