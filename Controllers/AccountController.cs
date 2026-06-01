using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using FootballPredictionGame.Data;

namespace FootballPredictionGame.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IFileUploadService _fileUploadService;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IFileUploadService fileUploadService,
        IEmailService emailService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _fileUploadService = fileUploadService;
        _emailService = emailService;
        _context = context;
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
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),

            Designation = model.Designation?.Trim(),
            Department = model.Department?.Trim(),

            FullName = model.FullName.Trim(),
            MobileNo = model.MobileNo.Trim(),

            ProfilePhotoPath = photoPath,

            IsActive = false,
            EmailConfirmed = false,

            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token)
            );

            var confirmationLink = Url.Action(
                action: nameof(ConfirmEmail),
                controller: "Account",
                values: new
                {
                    userId = user.Id,
                    token = encodedToken
                },
                protocol: Request.Scheme
            );

            var emailBody = $@"
            <div style='font-family:Arial, sans-serif; padding:20px; background:#f5f8ff;'>
                <div style='max-width:600px; margin:auto; background:#ffffff; padding:25px; border-radius:10px; border:1px solid #b9d7ff;'>
                    <h2 style='color:#0d6efd; margin-bottom:15px;'>Football Prediction Game</h2>

                    <p>Dear {user.FullName},</p>

                    <p>
                        Thank you for registering in the Football Prediction Game.
                        Please verify your email address by clicking the button below.
                    </p>

                    <p style='margin:25px 0;'>
                        <a href='{confirmationLink}'
                           style='background:#0d6efd; color:#ffffff; padding:12px 22px; 
                                  text-decoration:none; border-radius:6px; display:inline-block;'>
                            Verify Email Address
                        </a>
                    </p>

                    <p>
                        After email verification, admin will review and approve your account.
                    </p>

                    <p style='font-size:13px; color:#666;'>
                        If the button does not work, copy and paste this link into your browser:
                    </p>

                    <p style='font-size:13px; word-break:break-all; color:#0d6efd;'>
                        {confirmationLink}
                    </p>
                </div>
            </div>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Verify your Football Prediction Game account",
                emailBody
            );

            TempData["Success"] = "Registration successful. Please check your email and verify your account. After verification, admin will approve your login access.";

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        _fileUploadService.DeleteFileIfExists(photoPath);

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }


    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Invalid email verification request.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            TempData["Error"] = "User account was not found.";
            return RedirectToAction(nameof(Login));
        }

        if (user.EmailConfirmed)
        {
            TempData["Success"] = "Your email is already verified. Please wait for admin approval.";
            return RedirectToAction(nameof(Login));
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            TempData["Error"] = "Invalid email verification token.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(" ", result.Errors.Select(x => x.Description));
            TempData["Error"] = "Email verification failed. " + errorMessage;
            return RedirectToAction(nameof(Login));
        }

        // Extra safety: force update if Identity says success but DB still not updated
        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.Now;

        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Email verified successfully. Please wait for admin approval.";
        return RedirectToAction(nameof(Login));
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
        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError("", "Please verify your email address before login.");
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
    private string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private string HashOtp(string otp)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(bytes);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !user.EmailConfirmed)
        {
            ModelState.AddModelError("", "This email is not verified or not registered.");
            return View(model);
        }

        var oldOtps = _context.PasswordResetOtps
            .Where(x => x.UserId == user.Id && !x.IsUsed);

        foreach (var item in oldOtps)
            item.IsUsed = true;

        var otp = GenerateOtp();

        var passwordResetOtp = new PasswordResetOtp
        {
            UserId = user.Id,
            OtpHash = HashOtp(otp),
            ExpireAt = DateTime.Now.AddMinutes(10),
            IsUsed = false
        };

        _context.PasswordResetOtps.Add(passwordResetOtp);
        await _context.SaveChangesAsync();

        var emailBody = $@"
        <div style='font-family:Arial;padding:20px'>
            <h2>Password Reset OTP</h2>
            <p>Dear {user.FullName},</p>
            <p>Your password reset OTP is:</p>
            <h1 style='letter-spacing:5px;color:#0d6efd'>{otp}</h1>
            <p>This OTP will expire in 10 minutes.</p>
            <p>If you did not request this, please ignore this email.</p>
        </div>";

        await _emailService.SendEmailAsync(
            user.Email,
            "Football Prediction Password Reset OTP",
            emailBody
        );

        TempData["Success"] = "OTP has been sent to your verified email.";
        return RedirectToAction("ResetPasswordWithOtp", new { email = model.Email });
    }

    [HttpGet]
    public IActionResult ResetPasswordWithOtp(string email)
    {
        var model = new ResetPasswordWithOtpViewModel
        {
            Email = email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithOtp(ResetPasswordWithOtpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !user.EmailConfirmed)
        {
            ModelState.AddModelError("", "Invalid verified email address.");
            return View(model);
        }

        var otpHash = HashOtp(model.Otp);

        var validOtp = await _context.PasswordResetOtps
            .Where(x =>
                x.UserId == user.Id &&
                x.OtpHash == otpHash &&
                !x.IsUsed &&
                x.ExpireAt >= DateTime.Now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (validOtp == null)
        {
            ModelState.AddModelError("", "Invalid or expired OTP.");
            return View(model);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            resetToken,
            model.NewPassword
        );

        if (result.Succeeded)
        {
            validOtp.IsUsed = true;
            user.MustChangePassword = false;
            user.PasswordChangedAt = DateTime.Now;

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. Please login with your new password.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResendEmailVerification()
    {
        return View();
    }
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendEmailVerification(ResendEmailVerificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "No registered account found with this email address.");
            return View(model);
        }

        if (user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "This email address is already verified. Please login or contact admin if your account is waiting for approval.");
            return View(model);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token)
        );

        var confirmationLink = Url.Action(
            action: nameof(ConfirmEmail),
            controller: "Account",
            values: new
            {
                userId = user.Id,
                token = encodedToken
            },
            protocol: Request.Scheme
        );

        var emailBody = $@"
        <div style='font-family:Arial, sans-serif; padding:20px; background:#f5f8ff;'>
            <div style='max-width:600px; margin:auto; background:#ffffff; padding:25px; border-radius:10px; border:1px solid #b9d7ff;'>
                <h2 style='color:#0d6efd; margin-bottom:15px;'>Transtec 360° Football Prediction</h2>
                </br>
                <p>Dear {user.FullName},</p>

                <p>
                    You requested a new email verification link for your Transtec 360° Football Prediction account.
                </p>

                <p>
                    Please verify your email address by clicking the button below.
                </p>

                <p style='margin:25px 0;'>
                    <a href='{confirmationLink}'
                       style=' background:#ffc107;
            border:1px solid #e0a800;
            border-radius:8px;
            color:#212529 !important;
            display:inline-block;
            font-family:Arial,sans-serif;
            font-size:18px;
            font-weight:700;
            line-height:24px;
            padding:14px 32px;
            text-align:center;
            text-decoration:none;
            min-width:220px;'>
                        🛡️ Verify Email Address
                    </a>
                </p>

                <p>
                    After email verification, admin will review and approve your account.
                </p>

                <p style='font-size:13px; color:#666;'>
                    If the button does not work, copy and paste this link into your browser:
                </p>

                <p style='font-size:13px; word-break:break-all; color:#0d6efd;'>
                    {confirmationLink}
                </p>
            </div>
        </div>";

        await _emailService.SendEmailAsync(
            user.Email,
            "Resend Email Verification - Transtec 360° Football Prediction",
            emailBody
        );

        TempData["Success"] = "Verification link has been sent to your email address.";
        return RedirectToAction(nameof(Login));
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
