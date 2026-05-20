using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FootballPredictionGame.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IFileUploadService _fileUploadService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IFileUploadService fileUploadService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _fileUploadService = fileUploadService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? photoPath = null;
        try
        {
            if (model.ProfilePhoto != null)
            {
                photoPath = await _fileUploadService.SaveImageAsync(model.ProfilePhoto, "profiles");
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ProfilePhoto), ex.Message);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            Designation = model.Designation,
            Department = model.Department,
            FullName = model.FullName.Trim(),
            MobileNo = model.MobileNo.Trim(),
            ProfilePhotoPath = photoPath,
            IsActive = false,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            TempData["Success"] = "Registration successful. Your account is waiting for admin approval.";
            return RedirectToAction(nameof(Login));
        }

        _fileUploadService.DeleteFileIfExists(photoPath);

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account is waiting for admin approval.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (user.MustChangePassword)
            {
                return RedirectToAction(nameof(ForceChangePassword));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ForceChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!user.MustChangePassword)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Message = "For security, please change the default password before using the system.";
        return View(new ForceChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceChangePassword(ForceChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.CurrentPassword == model.NewPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "New password must be different from the default/current password.");
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Password changed successfully.";
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            MobileNo = user.MobileNo,
            Designation = user.Designation,
            Department = user.Department,
            Email = user.Email,
            ExistingPhotoPath = user.ProfilePhotoPath
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            model.ExistingPhotoPath = user.ProfilePhotoPath;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.MobileNo = model.MobileNo?.Trim();
        user.Designation = model.Designation?.Trim();
        user.Department = model.Department?.Trim();
        user.UpdatedAt = DateTime.Now;

        if (model.NewPhoto != null)
        {
            try
            {
                var newPath = await _fileUploadService.SaveImageAsync(model.NewPhoto, "profiles");
                _fileUploadService.DeleteFileIfExists(user.ProfilePhotoPath);
                user.ProfilePhotoPath = newPath;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.NewPhoto), ex.Message);
                model.ExistingPhotoPath = user.ProfilePhotoPath;
                return View(model);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is required to change password.");
                model.ExistingPhotoPath = user.ProfilePhotoPath;
                return View(model);
            }

            var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.ExistingPhotoPath = user.ProfilePhotoPath;
                return View(model);
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.ExistingPhotoPath = user.ProfilePhotoPath;
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
