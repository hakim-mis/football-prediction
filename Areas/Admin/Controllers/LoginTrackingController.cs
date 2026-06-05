using FootballPredictionGame.Data;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LoginTrackingController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoginTrackingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> ActiveUsers()
    {
        var model = await _context.UserActiveSessions
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.LastSeenAt)
            .Select(x => new UserActiveSessionViewModel
            {
                Id = x.Id,
                UserId = x.UserId,
                FullName = x.FullName ?? "",
                Email = x.Email ?? "",
                LoginAt = x.LoginAt,
                LastSeenAt = x.LastSeenAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                SessionId = x.SessionId
            })
            .ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> History(string? search, bool? successOnly, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.UserLoginHistories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(x =>
                (x.FullName != null && x.FullName.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)) ||
                (x.IpAddress != null && x.IpAddress.Contains(keyword)));
        }

        if (successOnly.HasValue)
        {
            query = query.Where(x => x.IsSuccess == successOnly.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.LoginAt >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endDate = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.LoginAt < endDate);
        }

        var model = await query
            .OrderByDescending(x => x.LoginAt)
            .Take(500)
            .Select(x => new UserLoginHistoryViewModel
            {
                Id = x.Id,
                UserId = x.UserId,
                FullName = x.FullName ?? "",
                Email = x.Email ?? "",
                LoginAt = x.LoginAt,
                LogoutAt = x.LogoutAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                SessionId = x.SessionId,
                IsSuccess = x.IsSuccess,
                FailureReason = x.FailureReason
            })
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.SuccessOnly = successOnly;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(model);
    }
}