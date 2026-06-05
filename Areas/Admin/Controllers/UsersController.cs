using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, IConfiguration configuration, 
        IEmailService emailService, ApplicationDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailService = emailService;
        _context = context;
    }

    public async Task<IActionResult> Index(string status = "all")
    {
        var users = await _userManager.Users
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var nonAdminUsers = new List<ApplicationUser>();

        foreach (var user in users)
        {
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                nonAdminUsers.Add(user);
            }
        }

        if (status == "pending")
        {
            nonAdminUsers = nonAdminUsers
                .Where(x => x.EmailConfirmed && !x.IsActive)
                .ToList();
        }
        else if (status == "active")
        {
            nonAdminUsers = nonAdminUsers
                .Where(x => x.IsActive)
                .ToList();
        }

        ViewBag.Status = status;

        var now = DateTime.Now;

        var publishedFixtures = await _context.Fixtures
            .Where(x => x.IsPublished)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.MatchDateTime,
                x.IsProcessed
            })
            .ToListAsync();

        var publishedFixtureIds = publishedFixtures
            .Select(x => x.Id)
            .ToList();

        var userIds = nonAdminUsers
            .Select(x => x.Id)
            .ToList();

        var predictions = await _context.Predictions
            .Where(x =>
                userIds.Contains(x.UserId) &&
                publishedFixtureIds.Contains(x.FixtureId))
            .Select(x => new
            {
                x.UserId,
                x.FixtureId
            })
            .ToListAsync();

        var predictionLookup = predictions
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.FixtureId).ToHashSet()
            );

        var model = nonAdminUsers.Select(user =>
        {
            var userPredictionFixtureIds = predictionLookup.ContainsKey(user.Id)
                ? predictionLookup[user.Id]
                : new HashSet<int>();

            var totalPredictionCount = userPredictionFixtureIds.Count;

            var notPredictCount = publishedFixtures.Count(fixture =>
                fixture.Status == MatchStatus.Upcoming &&
                !fixture.IsProcessed &&
                fixture.MatchDateTime > now &&
                !userPredictionFixtureIds.Contains(fixture.Id));

            var notParticipateCount = publishedFixtures.Count(fixture =>
                (fixture.Status == MatchStatus.Live ||
                 fixture.Status == MatchStatus.Finished ||
                 fixture.MatchDateTime <= now) &&
                !userPredictionFixtureIds.Contains(fixture.Id));

            return new UserManagementItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Designation = user.Designation,
                Department = user.Department,

                Email = user.Email ?? string.Empty,
                MobileNo = user.MobileNo,

                PhotoPath = user.ProfilePhotoPath,

                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,

                TotalScore = user.TotalScore,
                ExactPredictionCount = user.ExactPredictionCount,

                TotalPredictionCount = totalPredictionCount,
                NotPredictCount = notPredictCount,
                NotParticipateCount = notParticipateCount,

                CreatedAt = user.CreatedAt
            };
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Admin account status cannot be changed here.";
            return RedirectToAction(nameof(Index));
        }

        if (!user.EmailConfirmed)
        {
            TempData["Error"] = "User email is not verified yet. Approval is not allowed.";
            return RedirectToAction(nameof(Index), new { status = "pending" });
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.Now;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await SendAccountActivatedEmailAsync(user);
            TempData["Success"] = "User activated successfully and congratulation email sent.";
        }
        else
        {
            TempData["Error"] = "User activation failed.";
        }

        return RedirectToAction(nameof(Index), new { status = "pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Admin account status cannot be changed here.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await SendAccountDeactivatedEmailAsync(user);
            TempData["Success"] = "User deactivated successfully and notification email sent.";
        }
        else
        {
            TempData["Error"] = "User deactivation failed.";
        }

        return RedirectToAction(nameof(Index), new { status = "active" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Admin password cannot be reset from user management.";
            return RedirectToAction(nameof(Index));
        }

        var defaultPassword = _configuration["Security:DefaultResetPassword"] ?? "User@12345";

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["Error"] = "Password reset failed. Please check password policy or user status.";
            return RedirectToAction(nameof(Index));
        }

        var addResult = await _userManager.AddPasswordAsync(user, defaultPassword);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["Error"] = "Password reset failed. Default password does not meet policy.";
            return RedirectToAction(nameof(Index));
        }

        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.Now;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            TempData["Error"] = "Password reset completed, but user change-password status could not be updated.";
            return RedirectToAction(nameof(Index));
        }

        await SendAdminPasswordResetEmailAsync(user, defaultPassword);

        TempData["Success"] = "Password reset successfully. Default password email has been sent to the user.";
        return RedirectToAction(nameof(Index));
    }
    private async Task SendAccountActivatedEmailAsync(ApplicationUser user)
    {
        var motherUrl = _configuration["AppSettings:MotherUrl"] ?? "https://localhost:7042/";
        var loginUrl = motherUrl.TrimEnd('/') + "/Account/Login";

        var whatsAppGroupUrl = _configuration["AppSettings:WhatsAppGroupUrl"] ?? "";

        var whatsAppBlock = string.Empty;

        if (!string.IsNullOrWhiteSpace(whatsAppGroupUrl))
        {
            whatsAppBlock = $@"
        <div style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:12px;padding:18px;margin:25px 0;text-align:center;'>
            <div style='font-size:38px;margin-bottom:8px;'>💬</div>

            <h3 style='color:#15803d;margin:0 0 8px;font-size:20px;'>
                Join Our WhatsApp Group
            </h3>

            <p style='color:#334155;margin:0 0 16px;font-size:14px;line-height:1.6;'>
                Get match updates, prediction reminders, leaderboard news, and interact with other players.
            </p>

            <a href='{whatsAppGroupUrl}'
               style='background:#22c55e;border:1px solid #16a34a;border-radius:8px;
                      color:#ffffff !important;display:inline-block;font-family:Arial,sans-serif;
                      font-size:17px;font-weight:700;line-height:22px;padding:13px 28px;
                      text-align:center;text-decoration:none;min-width:230px;'>
                💬 Join WhatsApp Group
            </a>

            <p style='font-size:12px;color:#64748b;margin:14px 0 0;word-break:break-all;'>
                If the button does not work, copy this link:<br />
                <span style='color:#15803d;'>{whatsAppGroupUrl}</span>
            </p>
        </div>";
        }

        var emailBody = $@"
<div style='font-family:Arial,sans-serif;padding:20px;background:#f5f8ff;'>
    <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #b9d7ff;'>

        <div style='text-align:center;margin-bottom:20px;'>
            <div style='font-size:48px;'>🏆</div>
            <h2 style='color:#099b49;margin:10px 0 5px;'>Congratulations!</h2>
            <p style='color:#64748b;margin:0;'>Your account has been approved</p>
        </div>

        <p>Dear {user.FullName},</p>

        <p>
            Congratulations! Your <strong>Transtec 360° Football Prediction</strong> account has been activated by admin.
        </p>

        <p>
            You can now login, submit predictions, view fixtures, track your score, and compete on the leaderboard.
        </p>

        <p style='text-align:center;margin:30px 0;'>
            <a href='{loginUrl}'
               style='background:#ffc107;border:1px solid #e0a800;border-radius:8px;
                      color:#212529 !important;display:inline-block;font-family:Arial,sans-serif;
                      font-size:18px;font-weight:700;line-height:24px;padding:14px 32px;
                      text-align:center;text-decoration:none;min-width:220px;'>
                ⚽ Login Now
            </a>
        </p>

        {whatsAppBlock}

        <p style='font-size:13px;color:#64748b;'>
            Please follow the game rules and enjoy the prediction challenge.
        </p>

    </div>
</div>";

        await _emailService.SendEmailAsync(
            user.Email!,
            "Congratulations! Your Transtec 360° Football Prediction account is active",
            emailBody
        );
    }

    private async Task SendAccountDeactivatedEmailAsync(ApplicationUser user)
    {
        var emailBody = $@"
    <div style='font-family:Arial,sans-serif;padding:20px;background:#fff7ed;'>
        <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #fed7aa;'>

            <div style='text-align:center;margin-bottom:20px;'>
                <div style='font-size:48px;'>⚠️</div>
                <h2 style='color:#dc2626;margin:10px 0 5px;'>Account Deactivated</h2>
                <p style='color:#64748b;margin:0;'>Violation of game rules</p>
            </div>

            <p>Dear {user.FullName},</p>

            <p>
                Your <strong>Transtec 360° Football Prediction</strong> account has been deactivated by admin due to violation of the game rules.
            </p>

            <p>
                You will not be able to login or submit predictions while your account is inactive.
            </p>

            <p style='background:#fff7ed;border:1px solid #fed7aa;padding:14px;border-radius:8px;color:#7c2d12;'>
                If you believe this action was taken by mistake, please contact the game administrator.
            </p>

            <p style='font-size:13px;color:#64748b;'>
                This message was generated automatically by Transtec 360° Football Prediction.
            </p>

        </div>
    </div>";

        await _emailService.SendEmailAsync(
            user.Email!,
            "Your Transtec 360° Football Prediction account has been deactivated",
            emailBody
        );
    }

    private async Task SendAdminPasswordResetEmailAsync(ApplicationUser user, string defaultPassword)
    {
        //var loginUrl = Url.Action(
        //    action: "Login",
        //    controller: "Account",
        //    values: null,
        //    protocol: Request.Scheme
        //);
        var loginUrl = _configuration["AppSettings:MotherUrl"] + "Account/Login"
               ?? "";
        var emailBody = $@"
    <div style='font-family:Arial,sans-serif;padding:20px;background:#f5f8ff;'>
        <div style='max-width:650px;margin:auto;background:#ffffff;padding:28px;border-radius:12px;border:1px solid #b9d7ff;'>

            <div style='text-align:center;margin-bottom:20px;'>
                <div style='font-size:48px;'>🔐</div>
                <h2 style='color:#155dfc;margin:10px 0 5px;'>Password Reset by Admin</h2>
                <p style='color:#64748b;margin:0;'>Temporary login password issued</p>
            </div>

            <p>Dear {user.FullName},</p>

            <p>
                Your <strong>Transtec 360° Football Prediction</strong> account password has been reset by admin.
            </p>

            <p>
                Please login using the temporary password below. After login, you must set a new password before using the system.
            </p>

            <div style='background:#fff7ed;border:1px solid #fed7aa;border-radius:10px;padding:18px;text-align:center;margin:25px 0;'>
                <p style='margin:0 0 8px;color:#7c2d12;font-size:14px;font-weight:bold;'>One-Time Default Password</p>
                <div style='font-size:24px;font-weight:800;letter-spacing:1px;color:#111827;'>
                    {defaultPassword}
                </div>
            </div>

            <p style='text-align:center;margin:30px 0;'>
                <a href='{loginUrl}'
                   style='background:#ffc107;border:1px solid #e0a800;border-radius:8px;
                          color:#212529 !important;display:inline-block;font-family:Arial,sans-serif;
                          font-size:18px;font-weight:700;line-height:24px;padding:14px 32px;
                          text-align:center;text-decoration:none;min-width:220px;'>
                    🔐 Login & Change Password
                </a>
            </p>

            <p style='background:#f8fafc;border:1px solid #e2e8f0;padding:14px;border-radius:8px;color:#334155;'>
                For security, this password should be used only once. You will be required to create a new password immediately after login.
            </p>

            <p style='font-size:13px;color:#64748b;'>
                If you did not request this reset, please contact the game administrator immediately.
            </p>

        </div>
    </div>";

        await _emailService.SendEmailAsync(
            user.Email!,
            "Your Transtec 360° Football Prediction password has been reset",
            emailBody
        );
    }
}
