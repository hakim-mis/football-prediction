namespace FootballPredictionGame.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSize = 2 * 1024 * 1024;

    public FileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("Please select a valid image file.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Image size must not exceed 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG, JPEG, PNG and WEBP images are allowed.");
        }

        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public void DeleteFileIfExists(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return;
        }

        var relativePath = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
