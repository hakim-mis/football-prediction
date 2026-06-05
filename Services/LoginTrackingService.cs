// Services/LoginTrackingService.cs
using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services;

public class LoginTrackingService : ILoginTrackingService
{
    private readonly ApplicationDbContext _context;

    public LoginTrackingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task TrackSuccessfulLoginAsync(ApplicationUser user, HttpContext httpContext)
    {
        var sessionId = EnsureSessionId(httpContext);
        var ipAddress = GetIpAddress(httpContext);
        var userAgent = GetUserAgent(httpContext);

        var now = DateTime.Now;

        var loginHistory = new UserLoginHistory
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            LoginAt = now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            IsSuccess = true
        };

        _context.UserLoginHistories.Add(loginHistory);

        var existingActiveSessions = await _context.UserActiveSessions
            .Where(x =>
                x.UserId == user.Id &&
                x.SessionId == sessionId &&
                x.IsActive)
            .ToListAsync();

        foreach (var session in existingActiveSessions)
        {
            session.LastSeenAt = now;
            session.IpAddress = ipAddress;
            session.UserAgent = userAgent;
        }

        if (!existingActiveSessions.Any())
        {
            _context.UserActiveSessions.Add(new UserActiveSession
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                SessionId = sessionId,
                LoginAt = now,
                LastSeenAt = now,
                IsActive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task TrackFailedLoginAsync(string email, string reason, HttpContext httpContext)
    {
        var sessionId = EnsureSessionId(httpContext);

        var loginHistory = new UserLoginHistory
        {
            UserId = string.Empty,
            FullName = string.Empty,
            Email = email,
            LoginAt = DateTime.Now,
            IpAddress = GetIpAddress(httpContext),
            UserAgent = GetUserAgent(httpContext),
            SessionId = sessionId,
            IsSuccess = false,
            FailureReason = reason
        };

        _context.UserLoginHistories.Add(loginHistory);

        await _context.SaveChangesAsync();
    }

    public async Task TrackLogoutAsync(ApplicationUser user, HttpContext httpContext)
    {
        var sessionId = EnsureSessionId(httpContext);
        var now = DateTime.Now;

        var activeSessions = await _context.UserActiveSessions
            .Where(x =>
                x.UserId == user.Id &&
                x.SessionId == sessionId &&
                x.IsActive)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.IsActive = false;
            session.LogoutAt = now;
            session.LastSeenAt = now;
        }

        var latestLogin = await _context.UserLoginHistories
            .Where(x =>
                x.UserId == user.Id &&
                x.SessionId == sessionId &&
                x.IsSuccess &&
                x.LogoutAt == null)
            .OrderByDescending(x => x.LoginAt)
            .FirstOrDefaultAsync();

        if (latestLogin != null)
        {
            latestLogin.LogoutAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateHeartbeatAsync(ApplicationUser user, HttpContext httpContext)
    {
        var sessionId = EnsureSessionId(httpContext);
        var now = DateTime.Now;

        var activeSession = await _context.UserActiveSessions
            .Where(x =>
                x.UserId == user.Id &&
                x.SessionId == sessionId &&
                x.IsActive)
            .OrderByDescending(x => x.LoginAt)
            .FirstOrDefaultAsync();

        if (activeSession == null)
        {
            _context.UserActiveSessions.Add(new UserActiveSession
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                SessionId = sessionId,
                LoginAt = now,
                LastSeenAt = now,
                IsActive = true,
                IpAddress = GetIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext)
            });
        }
        else
        {
            activeSession.LastSeenAt = now;
            activeSession.IpAddress = GetIpAddress(httpContext);
            activeSession.UserAgent = GetUserAgent(httpContext);
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredSessionsAsync(int timeoutMinutes)
    {
        var now = DateTime.Now;
        var expiryTime = now.AddMinutes(-timeoutMinutes);

        var expiredSessions = await _context.UserActiveSessions
            .Where(x =>
                x.IsActive &&
                x.LastSeenAt < expiryTime)
            .ToListAsync();

        if (!expiredSessions.Any())
        {
            return;
        }

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.LogoutAt = now;
        }

        var sessionIds = expiredSessions
            .Select(x => x.SessionId)
            .ToList();

        var userIds = expiredSessions
            .Select(x => x.UserId)
            .ToList();

        var loginHistories = await _context.UserLoginHistories
            .Where(x =>
                x.IsSuccess &&
                x.LogoutAt == null &&
                sessionIds.Contains(x.SessionId!) &&
                userIds.Contains(x.UserId))
            .ToListAsync();

        foreach (var history in loginHistories)
        {
            history.LogoutAt = now;
        }

        await _context.SaveChangesAsync();
    }

    private static string EnsureSessionId(HttpContext httpContext)
    {
        const string sessionKey = "CurrentSessionId";

        var sessionId = httpContext.Session.GetString(sessionKey);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            httpContext.Session.SetString(sessionKey, sessionId);
        }

        return sessionId;
    }

    private static string? GetIpAddress(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserAgent(HttpContext httpContext)
    {
        return httpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }
}