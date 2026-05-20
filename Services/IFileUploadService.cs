namespace FootballPredictionGame.Services;

public interface IFileUploadService
{
    Task<string> SaveImageAsync(IFormFile file, string subFolder);
    void DeleteFileIfExists(string? webPath);
}
