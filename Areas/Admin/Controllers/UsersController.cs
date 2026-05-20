using FootballPredictionGame.Models;
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

    public UsersController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
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
            nonAdminUsers = nonAdminUsers.Where(x => !x.IsActive).ToList();
        }
        else if (status == "active")
        {
            nonAdminUsers = nonAdminUsers.Where(x => x.IsActive).ToList();
        }

        ViewBag.Status = status;

        var model = nonAdminUsers.Select(x => new UserManagementItemViewModel
        {
            Id = x.Id,
            FullName = x.FullName,
            Designation = x.Designation,
            Department = x.Department,
            Email = x.Email ?? string.Empty,
            MobileNo = x.MobileNo,
            PhotoPath = x.ProfilePhotoPath,
            IsActive = x.IsActive,
            TotalScore = x.TotalScore,
            ExactPredictionCount = x.ExactPredictionCount,
            CreatedAt = x.CreatedAt
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

        user.IsActive = true;
        user.UpdatedAt = DateTime.Now;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "User activated successfully.";
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
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "User deactivated successfully.";
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
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"Password reset successfully. Default password is {defaultPassword}. User must change it after login.";
        return RedirectToAction(nameof(Index));
    }

}
