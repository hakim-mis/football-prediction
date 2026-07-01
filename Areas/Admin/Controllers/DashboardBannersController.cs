using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardBannersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const long MaxImageSize = 2 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public DashboardBannersController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var banners = await _context.DashboardBanners
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new DashboardBannerViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Subtitle = x.Subtitle,
                ImageUrl = x.ImageUrl,
                ButtonText = x.ButtonText,
                RedirectUrl = x.RedirectUrl,
                IsActive = x.IsActive,
                Priority = x.Priority,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync();

        var model = new DashboardBannerIndexViewModel
        {
            Banners = banners,
            TotalBanners = banners.Count,
            ActiveBanners = banners.Count(x => x.IsActive),
            InactiveBanners = banners.Count(x => !x.IsActive)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new DashboardBannerFormViewModel
        {
            IsActive = true,
            Priority = 1,
            DisplayOrder = 1,
            ButtonText = "More Info"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DashboardBannerFormViewModel model)
    {
        if (model.ImageFile == null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Banner image is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var imageUrl = await SaveImageAsync(model.ImageFile!);

        var banner = new DashboardBanner
        {
            Title = model.Title.Trim(),
            Subtitle = model.Subtitle?.Trim(),
            ImageUrl = imageUrl,
            ButtonText = model.ButtonText?.Trim(),
            RedirectUrl = model.RedirectUrl?.Trim(),
            IsActive = model.IsActive,
            Priority = model.Priority,
            DisplayOrder = model.DisplayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.DashboardBanners.Add(banner);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Banner created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _context.DashboardBanners.FindAsync(id);

        if (banner == null)
        {
            return NotFound();
        }

        var model = new DashboardBannerFormViewModel
        {
            Id = banner.Id,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            ButtonText = banner.ButtonText,
            RedirectUrl = banner.RedirectUrl,
            IsActive = banner.IsActive,
            Priority = banner.Priority,
            DisplayOrder = banner.DisplayOrder,
            ExistingImageUrl = banner.ImageUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DashboardBannerFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var banner = await _context.DashboardBanners.FindAsync(id);

        if (banner == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingImageUrl = banner.ImageUrl;
            return View(model);
        }

        if (model.ImageFile != null)
        {
            DeleteImageFile(banner.ImageUrl);
            banner.ImageUrl = await SaveImageAsync(model.ImageFile);
        }

        banner.Title = model.Title.Trim();
        banner.Subtitle = model.Subtitle?.Trim();
        banner.ButtonText = model.ButtonText?.Trim();
        banner.RedirectUrl = model.RedirectUrl?.Trim();
        banner.IsActive = model.IsActive;
        banner.Priority = model.Priority;
        banner.DisplayOrder = model.DisplayOrder;
        banner.UpdatedAtUtc = DateTime.UtcNow;
        banner.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Banner updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var banner = await _context.DashboardBanners.FindAsync(id);

        if (banner == null)
        {
            return NotFound();
        }

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAtUtc = DateTime.UtcNow;
        banner.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = banner.IsActive
            ? "Banner activated successfully."
            : "Banner disabled successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _context.DashboardBanners.FindAsync(id);

        if (banner == null)
        {
            return NotFound();
        }

        DeleteImageFile(banner.ImageUrl);

        _context.DashboardBanners.Remove(banner);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Banner deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        ValidateImage(file);

        var uploadFolder = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "dashboard-banners");

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/dashboard-banners/{fileName}";
    }

    private static void ValidateImage(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG, JPEG, PNG, and WEBP images are allowed.");
        }

        if (file.Length > MaxImageSize)
        {
            throw new InvalidOperationException("Image size must be 2 MB or less.");
        }
    }

    private void DeleteImageFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var relativePath = imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}