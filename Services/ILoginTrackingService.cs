// Services/ILoginTrackingService.cs
using FootballPredictionGame.Models;

namespace FootballPredictionGame.Services;

public interface ILoginTrackingService
{
    Task TrackSuccessfulLoginAsync(ApplicationUser user, HttpContext httpContext);
    Task TrackFailedLoginAsync(string email, string reason, HttpContext httpContext);
    Task TrackLogoutAsync(ApplicationUser user, HttpContext httpContext);
    Task UpdateHeartbeatAsync(ApplicationUser user, HttpContext httpContext);
    Task CleanupExpiredSessionsAsync(int timeoutMinutes);
}